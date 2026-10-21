using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using SDPBC.EmailEngine;
using SDPBC.Employees.ServiceConnector;
using SDPBC.Svc.Common.GlobalConfig;
using SDPBC.Svc.Common.Logging;
using System.Text.Json;
using WRKJRNL01.Data.Models;
using WRKJRNL01.Data.Repository;
using WRKJRNL01.Data.Services;
using WRKJRNL01.Notifications.ScheduledTasks;

namespace WRKJRNL01.Notifications
{
    internal class Worker : BackgroundService
    {
        private readonly IConfiguration configuration;
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly SDPBC.Svc.Common.TaskScheduler.TaskScheduler scheduler =
            new SDPBC.Svc.Common.TaskScheduler.TaskScheduler();

        private readonly IServiceCollection services = new ServiceCollection();

        public Worker(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private IServiceProvider? serviceProvider;

        private void InitializeWorkerServices()
        {
            services.Clear();
            serviceProvider = null;

            #region Email Engine

            var mailConfig = AppSettings.Configuration.GetRequiredSection("EmailEngine");

            services.AddSingleton(
                new EmailEngine(
                    mailConfig.GetValue<string>("server") ?? string.Empty,
                    mailConfig.GetValue<int>("port"),
                    mailConfig.GetValue<bool>("testing"),
                    mailConfig.GetValue<bool>("simulation_mode"),
                    mailConfig.GetValue<string>("simulation_redirects")?.Split("|") ?? Array.Empty<string>()));

            #endregion

            #region Employee Service

            var employeeServiceConfig = AppSettings.Configuration.GetRequiredSection("EmployeesSVC");

            services.AddSingleton( new EmployeeServiceConnector(
                    employeeServiceConfig["BaseURL"],
                    employeeServiceConfig["APIKey"]));

            #endregion

            #region Database

            string dbConnectionString = AppSettings.Configuration.GetConnectionString("2026WRKJRNL01") ?? string.Empty;

            services.AddPooledDbContextFactory<WRKJRNL01Context>(options => options
                        .UseSqlServer(dbConnectionString)
                        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

            services.AddTransient<CatRepo>();
            services.AddTransient<TaskRepo>();
            services.AddTransient<UserRepo>();

            #endregion

            #region Services

            services.AddSingleton<IConfiguration>(configuration);

            services.AddDataProtection();

            services.AddTransient<EmployeeResolve>();
            services.AddTransient<GoogleConnectRepo>();
            services.AddTransient<TokenEncryptionService>();
            services.AddTransient<GoogleCalendarService>();
            services.AddTransient<HolidayRepo>();
            #endregion

            serviceProvider = services.BuildServiceProvider();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            LogFormatter.GenericMessage(logger, LogFormatterLevel.Info, "Worker has been started...");

            LogFormatter.GenericMessage(logger, LogFormatterLevel.Info, "Initializing dependencies...");

            InitializeWorkerServices();

            LogFormatter.GenericMessage(logger, LogFormatterLevel.Info, "Registering scheduled tasks...");

            scheduler.RegisterTask(new LogHeartbeat("HeartBeat"));

            scheduler.RegisterTask(new MissingJournalReminder("MissingJournalReminder", serviceProvider!));

            LogFormatter.GenericMessage(logger, LogFormatterLevel.Info, "Ready.");

            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            LogFormatter.GenericMessage( logger,LogFormatterLevel.Info, "Worker has been stopped...");

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await scheduler.RunTasks();

                await Task.Delay(10000, stoppingToken);
            }
        }
    
    }
}