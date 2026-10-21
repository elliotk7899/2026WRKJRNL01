using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Extensions.Logging;
using SDPBC.Svc.Common.GlobalConfig;

namespace WRKJRNL01.Notifications
{
    public class Program
    {
        private static readonly NLog.ILogger logger =
            LogManager.GetCurrentClassLogger();

        public static async Task Main(string[] args)
        {
            try
            {
                var builder = Host.CreateDefaultBuilder(args);
                IHost host;

                // Push service identity to the logger.
                ScopeContext.PushProperty("User", "SVC-WRKR");

                // Configure as a Windows Service.
                builder.UseWindowsService(options =>
                {
                    options.ServiceName = "WRKJRNL01 Notification Service";
                });

                // Register worker.
                builder.ConfigureServices(services =>
                {
                    services.AddHostedService<Worker>();
                });

                // Build host.
                host = builder.Build();

                // Make configuration available through AppSettings.
                AppSettings.Configuration = host.Services.GetRequiredService<IConfiguration>();

                // Configure NLog from appsettings.
                LogManager.Configuration =
                    new NLogLoggingConfiguration(AppSettings.Configuration.GetRequiredSection("NLog"));

                logger.Info("Service is starting up...");

                logger.Info("Configuration Loaded: " + AppSettings.Configuration.GetValue<string>("ConfigName"));

                await host.RunAsync();

                logger.Info("Service is shutting down...");
            }
            catch (Exception ex)
            {
                logger.Fatal(ex,"An error occurred during service setup / run.");

                throw;
            }
        }
    }
}