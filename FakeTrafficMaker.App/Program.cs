using FakeTrafficMaker.App.Core;

using Serilog;
using Serilog.Debugging;

namespace FakeTrafficMaker.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddEnvironmentVariables()
                    .AddJsonFile("appsettings.json", true)
                    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true)
                    .Build();

            //builder.Services.AddLogging(x => x.AddConfiguration());
            builder.Services.AddSerilog(opt =>
            {
                opt.ReadFrom.Configuration(configuration);
            });
            SelfLog.Enable(Console.Error);

            Settings settings = new Settings();
            var settingConfigurationSection = configuration.GetSection("Settings");
            settingConfigurationSection.Bind(settings);
            if (args.Length > 0)
                settings.ParseArguments(args);
            builder.Services.AddOptions().AddSingleton<Settings>(settings);

            builder.Services.AddHttpClient("default", opt =>
            {
                opt.DefaultRequestHeaders.Clear();
                // a fake user agent like Chrome
                opt.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.142.86 Safari/537.36");
                // set all http client requests use HTTP 2 and HTTP 1.1
                opt.DefaultRequestVersion = new Version(2, 0);
                opt.MaxResponseContentBufferSize = 8 * 1000 * 1024; // 8mb
                opt.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();

            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Serilog.Log.Fatal(ex, "Host Crashed!");
                Serilog.Log.CloseAndFlush();
                throw;
            }

            Console.WriteLine("Program finished.");
        }

    }

}