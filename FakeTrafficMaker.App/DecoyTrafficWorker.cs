using FakeTrafficMaker.App.Core;

using System.Collections.Concurrent;
using System.Net.Http.Headers;

namespace FakeTrafficMaker.App
{
    public class DecoyTrafficWorker : BackgroundService
    {
        private readonly ILogger<DecoyTrafficWorker> _logger;
        private readonly Settings _settings;
        private readonly HttpClient _client;
        private readonly SemaphoreSlim _cuncurrentSemaphore;
        private readonly Timer _resourceGarbagaerTimer;
        private readonly CircuitBreakerHandler _circuitBreakerHandler;
        public DecoyTrafficWorker(ILogger<DecoyTrafficWorker> logger, Settings settings, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _settings = settings;
            _client = clientFactory.CreateClient("default");

            _cuncurrentSemaphore = new SemaphoreSlim(_settings.Concurrency);
            _circuitBreakerHandler = new CircuitBreakerHandler(failureThreshold: 2, openTimeout: TimeSpan.FromSeconds(15));
            _resourceGarbagaerTimer = new Timer(GarbagerTimerCallback, null, TimeSpan.FromMinutes(15).Milliseconds, TimeSpan.FromMinutes(15).Milliseconds);
        }

        /// <summary>
        /// worker runner
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Decoy TrafficMaker Worker running at: {time}", DateTimeOffset.Now);
                }
                // check network availability and internet access first and delay if no internet access or network is not available
                if (!await Utilities.IsInternetAvailable())
                {
                    _logger.LogWarning("FakeTrafficMaker Worker => Network / Internet is not accessible . Sleeping...");
                    await WaitForNextActivationSession(stoppingToken).ConfigureAwait(false);
                    continue;
                }

                List<Task> tasks = new List<Task>
                {
                    // Send requests to different destinations
                    SendRequestsAsync(stoppingToken),
                    // Upload to different destinations
                    UploadAsync(stoppingToken)
                };
                await Task.WhenAll(tasks).ConfigureAwait(false);

                _logger.LogInformation("All FakeTrafficMaker Tasks Completed.");
                await WaitForNextActivationSession(stoppingToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Worker restart delay
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private async Task WaitForNextActivationSession(CancellationToken stoppingToken)
        {
            var activationAgainTime = Random.Shared.Next(_settings.DelayMinutes, _settings.ActivationTimesMultipler[Random.Shared.Next(0, _settings.ActivationTimesMultipler.Length)]);

            switch (_settings.ActivationTimes)
            {
                case 1:
                    // add a delay task with minutes
                    _logger.LogInformation("Sleeping for {activationAgainTime} minutes to active again...", activationAgainTime);
                    await Task.Delay(TimeSpan.FromMinutes(activationAgainTime), stoppingToken).ConfigureAwait(false);
                    break;
                case 2:
                    // add a delay task with hours
                    _logger.LogInformation("Sleeping for {activationAgainTime} hours to active again...", activationAgainTime);
                    await Task.Delay(TimeSpan.FromHours(activationAgainTime), stoppingToken).ConfigureAwait(false);
                    break;
                default:
                    // add a delay task with seconds
                    _logger.LogInformation("Sleeping for {activationAgainTime} seconds to active again...", activationAgainTime);
                    await Task.Delay(TimeSpan.FromSeconds(activationAgainTime), stoppingToken).ConfigureAwait(false);
                    break;
            }
        }

        /// <summary>
        /// tcp http request maker job (+ a few send & recieive traffic)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendRequestsAsync(CancellationToken cancellationToken = default)
        {
            var destinations = _settings.RequestDestinations;
            if (destinations.Count == 0)
            {
                _logger.LogError("No request destination specified! skipping");
                return;
            }
            List<string> shuffledDestinations = destinations.OrderBy(d => Random.Shared.Next()).ToList();

            await Parallel.ForEachAsync(shuffledDestinations.Take(Random.Shared.Next(1, destinations.Count)),
                new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = _settings.Concurrency }
                , async (destination, ct) =>
                {
                    #region without circut breaker
                    //try
                    //{
                    //    using var response = await _client.GetAsync(destination, cancellationToken).ConfigureAwait(false);
                    //    _logger.LogInformation("FakeRequester => Response from {destination} code: {statusCode} size: {size} KB",
                    //                           destination, response.StatusCode,
                    //                           response.Content.Headers.ContentLength / 1024);
                    //}
                    //catch (Exception ex)
                    //{
                    //    _logger.LogError(ex, "FakeRequester => Error accessing {destination} . Delaying...", destination);
                    //    await Task.Delay(30000, cancellationToken).ConfigureAwait(false);
                    //}
                    #endregion

                    // make requests in circut breaker mode and failure handling
                    try
                    {
                        await _circuitBreakerHandler.ExecuteAsync(async () =>
                        {
                            using var response = await _client.GetAsync(destination, cancellationToken).ConfigureAwait(false);
                            _logger.LogInformation("FakeRequester => Response from {destination} code: {statusCode} size: {size} KB",
                                                   destination, response.StatusCode,
                                                   response.Content.Headers.ContentLength / 1024);
                        });
                    }
                    catch (CircuitBreakerOpenException circuitEx)
                    {
                        _logger.LogError(circuitEx, "FakeRequester => Circuit breaker state is {circuitState}. preventing new operations untill {circuitTimeout}", _circuitBreakerHandler.State, _circuitBreakerHandler.CircuitResetTime);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "FakeRequester => Error accessing {destination}. Delaying...", destination);

                    }
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// upload traffic maker job
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UploadAsync(CancellationToken cancellationToken = default)
        {
            var destinations = _settings.UploadDestinations;
            if (destinations.Count == 0)
            {
                _logger.LogError("FakeTrafficUploader => No upload destination specified! skipping job");
                return;
            }

            ConcurrentQueue<Task> tasks = new ConcurrentQueue<Task>();

            foreach (var destination in destinations)
            {
                await _cuncurrentSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                tasks.Enqueue(Task.Run(async () =>
                        {
                            byte[] randomDataBytes = null;
                            try
                            {
                                await _circuitBreakerHandler.ExecuteAsync(async () =>
                                {
                                    randomDataBytes = Utilities.GenerateRandomDataBuffer(Random.Shared.Next(_settings.MinDataSize, _settings.MinDataSize * _settings.Multiplier), _settings.Multiplier);

                                    //var randomBandwidth = 500 * Random.Shared.Next(_settings.MinDataSize, _settings.MinDataSize * _settings.Multiplier); // Base speed 500, multiplier 1-100
                                    // ... (Calculate timeout based on bandwidth and data size)
                                    _logger.LogInformation("FakeTrafficUploader => Uploading to {destination}: {size} KB", destination, randomDataBytes.Length / 1024);

                                    using (ByteArrayContent content = new ByteArrayContent(randomDataBytes))
                                    {

                                        content.Headers.ContentLength = randomDataBytes.Length;
                                        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                                        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = Guid.NewGuid() + ".bin" };
                                        content.Headers.ContentDisposition.Size = randomDataBytes.Length;
                                        // handle timeout or socket close problem when large data is uploading to server
                                        using var response = await _client.PostAsync(destination, content, cancellationToken).ConfigureAwait(false);
                                        if (!response.IsSuccessStatusCode)
                                        {
                                            _logger.LogError("FakeTrafficUploader => Upload error {destination}", destination);
                                            return;
                                        }
                                        _logger.LogDebug("FakeTrafficUploader => Uploaded to {destination}: {StatusCode} {size} KB", destination, response.StatusCode, randomDataBytes.Length / 1024);
                                        // free random data array memory after upload
                                    }
                                });
                            }
                            catch (CircuitBreakerOpenException ex)
                            {
                                _logger.LogError(ex, "FakeTrafficUploader => Circuit breaker state is {circuitState}. preventing new operations untill {circuitTimeout}", _circuitBreakerHandler.State, _circuitBreakerHandler.CircuitResetTime);
                                // Handle open circuit breaker state
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "FakeTrafficUploader => Upload error {destination}", destination);
                                //await Task.Delay(5000);
                            }
                            finally
                            {
                                // return buffer to pool
                                Utilities.ReturnRandomDataBuffer(randomDataBytes);
                                _cuncurrentSemaphore.Release();
                            }
                        }, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }

        #region Internal Service Methods

        /// <summary>
        /// run on worker stop call
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _client.CancelPendingRequests();
            _logger.LogWarning("Stopping FakeTrafficMaker Worker...");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// run on worker dispose to release resources and close
        /// </summary>
        public override void Dispose()
        {
            _client?.Dispose();
            _cuncurrentSemaphore?.Dispose();
            _resourceGarbagaerTimer?.Dispose();
            _circuitBreakerHandler?.Dispose();

            base.Dispose();
        }

        /// <summary>
        /// memory cleanup timer action
        /// </summary>
        /// <param name="state"></param>
        private void GarbagerTimerCallback(object? state)
        {
            RequestGcToFreeMemory();
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("GarbagerTimerCallback = > Memory freed");
        }

        /// <summary>
        /// force GC to collect and free memory
        /// </summary>
        private void RequestGcToFreeMemory()
        {
            // call GC to clean up unused objects
            GC.Collect();
            // wait for finalizer
            GC.WaitForPendingFinalizers();
            // call GC again to collect all generations
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }
        #endregion
    }
}
