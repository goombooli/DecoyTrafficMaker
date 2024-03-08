namespace FakeTrafficMaker.App.Core
{
    /// <summary>
    /// app settings which bond to appsettings.json file at startup
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Default concurrency level
        /// </summary>
        public int Concurrency { get; set; } = 2;
        /// <summary>
        /// short circuit threshold to delay new operations if any error is detected
        /// </summary>
        public int CircuitBreakerThreshold { get; set; } = 3;
        /// <summary>
        /// circuit breaker timeout in seconds
        /// </summary>
        public int CircuitBreakerTimeout { get; set; } = 60;
        /// <summary>
        /// active jobs interval. secondly = 0, minutely = 1 / hourly = 2 / daily = 3
        /// </summary>
        public int ActivationTimes { get; set; } = 0;
        /// <summary>
        /// jobs activation times multiplier. randomly <see cref="Settings.ActivationTimes"/> * <see cref="Settings.ActivationTimesMultipler"/>
        /// </summary>
        public int[] ActivationTimesMultipler { get; set; } = [5, 10, 15, 20, 30];
        /// <summary>
        /// base data size in bytes to upload. actual size = <see cref="Settings.MinDataSize"/> * <see cref="Settings.Multiplier"/> randomly.
        /// </summary>
        public int MinDataSize { get; set; } = 50000;
        /// <summary>
        /// data size multiplier. default = 10
        /// </summary>
        public int Multiplier { get; set; } = 10;
        /// <summary>
        /// delay interval between jobs in minutes. actual delay = <see cref="Settings.DelayMinutes"/> * <see cref="Settings.ActivationTimesMultipler"/> randomly.
        /// </summary>
        public int DelayMinutes { get; set; } = 1;
        /// <summary>
        /// urls to send tcp requests. choosen randomly each time
        /// </summary>
        public List<string> RequestDestinations { get; set; } = [];
        /// <summary>
        /// urls to send tcp traffic. choosen randomly each time
        /// </summary>
        public List<string> UploadDestinations { get; set; } = [];
        /// <summary>
        /// utls to download data. choosen randomly each time
        /// </summary>
        public List<string> DownloadDestinations { get; set; } = [];

        public string DefaultClientUserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.142.86 Safari/537.36";
        public int DefaultClientTimeout { get; set; } = 60;
    }
}
