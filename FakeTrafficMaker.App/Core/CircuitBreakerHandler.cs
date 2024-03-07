namespace FakeTrafficMaker.App.Core
{
    public class CircuitBreakerHandler : IDisposable
    {
        /// <summary>
        /// time to prevent new operations while circuit breaker is open, in seconds
        /// </summary>
        private const int DefaultCircuitBreakerOpenTimeSeconds = 60;

        private readonly int _failureThreshold;
        private readonly TimeSpan _openTimeout;
        /// <summary>
        /// total times when circuit breaker was tripped
        /// </summary>
        private int _failureCount = 0;
        /// <summary>
        /// last time when circuit breaker was tripped
        /// </summary>
        private DateTime _lastFailureTime = DateTime.MinValue;
        public DateTime CircuitResetTime => _lastFailureTime.Add(_openTimeout);
        /// <summary>
        /// circuit breaker state - closed, open, half open
        /// </summary>
        public CircuitState State { get => _state; set => _state = value; }
        private CircuitState _state = CircuitState.Closed;
        private bool _disposedValue;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);


        public CircuitBreakerHandler(int failureThreshold = 3, TimeSpan openTimeout = default)
        {
            _failureThreshold = failureThreshold;
            _openTimeout = openTimeout == default ? TimeSpan.FromSeconds(DefaultCircuitBreakerOpenTimeSeconds) : openTimeout;
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            if (_state == CircuitState.Open && DateTime.Now - _lastFailureTime > _openTimeout)
            {
                if (await _semaphore.WaitAsync(0))
                {
                    try
                    {
                        if (_state == CircuitState.Open && DateTime.Now - _lastFailureTime > _openTimeout)
                        {
                            _state = CircuitState.HalfOpen;
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }

            if (_state == CircuitState.Closed || _state == CircuitState.HalfOpen)
            {
                try
                {
                    await action();
                    if (_state == CircuitState.HalfOpen)
                    {
                        Reset();
                    }
                }
                catch (Exception)
                {
                    Trip();
                    throw;
                }
            }
            else
            {
                throw new CircuitBreakerOpenException($"Circuit breaker is open untill {CircuitResetTime}");
            }
        }

        private void Trip()
        {
            if (_semaphore.Wait(0))
            {
                try
                {
                    _failureCount++;
                    if (_failureCount >= _failureThreshold)
                    {
                        _state = CircuitState.Open;
                        _lastFailureTime = DateTime.Now;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        private void Reset()
        {
            if (_semaphore.Wait(0))
            {
                try
                {
                    _state = CircuitState.Closed;
                    _failureCount = 0;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    Reset();
                    _semaphore?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message)
        {
        }
    }
}
