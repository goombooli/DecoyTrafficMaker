using FakeTrafficMaker.App.Core;

using Serilog;
using Serilog.Debugging;

namespace FakeTrafficMaker.App
{
    public class Program
    {
        public static async Task Main(string[] args)
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
            // read settings from appsettings.json file and bind it
            var settingConfigurationSection = configuration.GetSection("Settings");
            settingConfigurationSection.Bind(settings);
            // set command line args if present
            if (args.Length > 0)
                settings.ParseArguments(args);

            builder.Services.AddOptions().AddSingleton<Settings>(settings);

            builder.Services.AddHttpClient("default", opt =>
            {
                opt.DefaultRequestHeaders.Clear();
                // a fake user agent like Chrome
                opt.DefaultRequestHeaders.Add("User-Agent", settings.DefaultClientUserAgent); // Get Http Client User-Agent from settings
                // set all http client requests use HTTP 2 and HTTP 1.1
                opt.DefaultRequestVersion = new Version(2, 0);
                opt.MaxResponseContentBufferSize = 10 * 1000 * 1024; // 10mb
                opt.Timeout = TimeSpan.FromSeconds(settings.DefaultClientTimeout);
            });
            builder.Services.AddHostedService<DecoyTrafficWorker>();

            var host = builder.Build();

            try
            {
                Utilities.PrintCommandLineHelp();
                Serilog.Log.ForContext<Program>().Information("Warmup DecoyTrafficMaker Host 5 seconds...");
                await Task.Delay(5000);

                Serilog.Log.Information("Warmup Done, DecoyTrafficMaker Host Starting!");

                host.Run();
            }
            catch (Exception ex)
            {
                Serilog.Log.Fatal(ex, "DecoyTrafficMaker Host Crashed Unexpectly!");
                Serilog.Log.CloseAndFlush();
                throw;
            }

            Console.WriteLine("DecoyTrafficMaker Program finished.");
        }

    }

}