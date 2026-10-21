using _2026WRKJRNL01.Shared.Services;
using _2026WRKJRNL01.UI.Components;
using _2026WRKJRNL01.UI.Components.Auth;
using _2026WRKJRNL01.UI.Components.Settings;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using SDPBC.EmailEngine;
using System.Globalization;
using System.Reflection;
using WRKJRNL01.Data.Models;
using WRKJRNL01.Data.Repository;
using WRKJRNL01.Data.Services;
using WRKJRNL01.UI.Components.Settings;

namespace _2026WRKJRNL01.UI
{
    public class Program
    {
        // TODO:  This specifies the app's revision data!  Update this during maintenance.
        public static string? AppRevision { get { return "Prototype R0"; } }
        public static long UploadMaxFileSize { get; private set; }
        public static string? EnvironmentName { get; private set; }
        public static string? AppId { get; private set; }
        public static string? AppVersion { get; private set; }
        public static string? AppName { get; private set; }
        public static DateTime BuildDate { get; private set; }

        private static long? version;

        public static bool ClarityEnabled { get; private set; }
        public static string ClarityID { get; private set; } = string.Empty;

        public static string LogoURL { get; set; } = string.Empty;

        public static async Task Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                #region Setup App Version and Environment
                // Set our environment variable as defined in the configuration.
                EnvironmentName = builder.Configuration.GetValue<string>("Environment")!;

                // Get app id
                AppId = builder.Configuration.GetValue<string>("AppId");

                // Get app name
                AppName = builder.Configuration.GetValue<string>("AppName");

                BuildDate = GetBuildDate(Assembly.GetExecutingAssembly());

                // Load in Logo URL
                LogoURL = builder.Configuration.GetValue<string>("LogoURL");

                #endregion

                #region Email Engine
                var mailConfig = builder.Configuration.GetRequiredSection("EmailEngine");

                builder.Services.AddSingleton<EmailEngine>(new EmailEngine(
                                                               mailConfig.GetValue<string>("server"),
                                                               mailConfig.GetValue<int>("port"),
                                                               mailConfig.GetValue<bool>("testing"),
                                                               mailConfig.GetValue<bool>("simulation_mode"),
                                                               mailConfig.GetValue<string>("simulation_redirects").Split("|"))
                                                               );

                #endregion

                #region Setup Caching

                // Setup our memory cache for the auth package.
                builder.Services.AddMemoryCache(options => options.ExpirationScanFrequency = TimeSpan.FromSeconds(30));

                #endregion

                #region Setup Local Auth (OFFLINE)

                // NETWORK AUTH REMOVED FOR LOCAL/OFF-NETWORK USE.
                // The SDPBC SAML authentication package, Redis ticket store, Google OAuth,
                // and the employee web service have all been removed. In their place we use
                // a single configurable local user (see the "LocalUser" section in
                // appsettings and LocalUserProvider / LocalAuthenticationStateProvider).

                // The hardcoded local dev user.
                builder.Services.AddSingleton<LocalUserProvider>();

                // Feed <AuthorizeView> / [Authorize] from the local user's claims.
                builder.Services.AddScoped<AuthenticationStateProvider, LocalAuthenticationStateProvider>();
                builder.Services.AddAuthorization();
                builder.Services.AddCascadingAuthenticationState();

                #endregion

                #region Setup services and Razor components.
                //added for web service calls
                builder.Services.AddHttpClient();

                //service for making HTTP requests
                builder.Services.AddScoped<WebServiceClient>();

                // App state container for themes and such
                builder.Services.AddScoped<AppState>();
                builder.Services.AddScoped<LayoutState>();
                builder.Services.AddSingleton<AppVersionService>();

                // DATA PROTECTION (OFFLINE)
                // Azure Key Vault + Azure AD key protection removed. Keys are persisted to a
                // local folder so DataProtection (used by TokenEncryptionService) works with
                // no network/Azure dependency.
                var keyRingPath = Path.Combine(AppContext.BaseDirectory, "dp-keys");
                Directory.CreateDirectory(keyRingPath);

                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
                    .SetApplicationName("WRKJRNL01");

                builder.Services.AddSingleton<TokenEncryptionService>();
                //////////////////////////////////

                //add controller support
                builder.Services.AddControllers();

                // Add services to the container.
                builder.Services.AddRazorComponents()
                    .AddInteractiveServerComponents();

                // Add MudBlazor services
                builder.Services.AddMudServices(config =>
                {
                    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;

                    config.SnackbarConfiguration.PreventDuplicates = false;
                    config.SnackbarConfiguration.NewestOnTop = false;
                    config.SnackbarConfiguration.ShowCloseIcon = true;
                    config.SnackbarConfiguration.VisibleStateDuration = 5000;
                    config.SnackbarConfiguration.HideTransitionDuration = 500;
                    config.SnackbarConfiguration.ShowTransitionDuration = 500;
                    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
                });

                // https://stackoverflow.com/questions/58436497/what-are-asp-net-core-static-web-assets
                builder.WebHost.UseStaticWebAssets();
                #endregion

                #region Database

                // LOCAL DATABASE (OFFLINE): SQLite instead of the on-network SQL Server.
                // The connection string defaults to a local file next to the app if not set
                // in configuration, so the app is fully self-contained.
                string dbConnectionString = builder.Configuration.GetConnectionString("2026WRKJRNL01")
                    ?? $"Data Source={Path.Combine(AppContext.BaseDirectory, "2026WRKJRNL01.db")}";

                //create database context
                builder.Services.AddPooledDbContextFactory<WRKJRNL01Context>(options => options.UseSqlite(dbConnectionString).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution));

                // Setup our data repositories.
                builder.Services.AddScoped<CatRepo>();
                builder.Services.AddScoped<TaskRepo>();
                builder.Services.AddScoped<TaskService>();
                builder.Services.AddScoped<GoogleConnectRepo>();
                builder.Services.AddScoped<GoogleCalendarService>();
                #endregion

                #region New Logging 
                // Setup logging providers.
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole();

                //get dat debug
                if (EnvironmentName == "Debug" || EnvironmentName == "Development" || EnvironmentName == "Testing")
                {
                    builder.Logging.AddDebug();
                }

                // OpenTelemetry OTLP export removed for off-network use (it targeted an
                // internal collector endpoint that is unreachable locally). Console/Debug
                // logging above is sufficient for local runs.

                builder.Logging.SetMinimumLevel(LogLevel.Information);

                //build the app
                var app = builder.Build();

                // LOCAL DB BOOTSTRAP (OFFLINE): create the SQLite database and schema on
                // first run so there is nothing to set up manually. Uses EnsureCreated
                // (no migrations) which is appropriate for a self-contained local build.
                using (var scope = app.Services.CreateScope())
                {
                    var ctxFactory = scope.ServiceProvider
                        .GetRequiredService<IDbContextFactory<WRKJRNL01Context>>();
                    using var ctx = ctxFactory.CreateDbContext();
                    ctx.Database.EnsureCreated();
                }

                var versionSvc = app.Services.GetRequiredService<AppVersionService>();

                //set version env var
                AppVersion = versionSvc.Version;

                //get the logging services
                var logger = app.Services.GetRequiredService<ILogger<Program>>();

                // Log our configured environment, or throw an error if none exists in configuration.
                if (!string.IsNullOrWhiteSpace(EnvironmentName))
                {
                    logger.LogInformation($"Configured environment is {EnvironmentName}");
                }
                else
                {
                    throw new ArgumentException("Environment name is not set in configuration!");
                }

                //logging regardless of env
                logger.LogInformation("Application is starting up...");
                logger.LogInformation($"Application Name: {AppName}");
                logger.LogInformation($"Application ID: {AppId}");
                logger.LogInformation($"Version: {AppVersion}");
                logger.LogInformation($"Revision: {AppRevision}");
                logger.LogInformation($"Build Date: {BuildDate}");
                logger.LogInformation($"Env: {EnvironmentName}");

                #endregion

                #region App Setup
                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }

                app.UseDeveloperExceptionPage();
                app.UseHttpsRedirection();

                app.UseStaticFiles();
                app.UseAntiforgery();

                // Local authorization only. SAML authentication (app.UseAuthentication) and
                // the SDPBC cookie scrubber middleware were removed for off-network use.
                app.UseAuthorization();

                //for controllers
                app.MapControllers();

                //this is required for our env
                app.MapRazorComponents<App>()
                    .AddInteractiveServerRenderMode(o => o.DisableWebSocketCompression = true);

                app.Run();
                #endregion
            }
            catch (Exception ex)
            {
                //LogFormatter.Error(logger, "An error occurred during startup!", ex);
                throw;
            }
        }

        private static long GetAssemblyFileDate()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileInfo fi = new(assembly.Location);
            return fi.LastWriteTime.Ticks;
        }

        public static long VersionDate
        {
            get
            {
                version ??= GetAssemblyFileDate();
                return (long)version;
            }
        }

        static DateTime GetBuildDate(Assembly assembly)
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value.Substring(index + BuildVersionMetadataPrefix.Length);
                    if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }
    }
}
