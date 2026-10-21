using NLog;
using SDPBC.Svc.Common.Logging;
using SDPBC.Svc.Common.TaskScheduler;

namespace WRKJRNL01.Notifications.ScheduledTasks
{
    internal class LogHeartbeat : ScheduledTaskBase
    {
        private readonly NLog.ILogger logger =
            LogManager.GetCurrentClassLogger();

        public LogHeartbeat(string taskName) : base(taskName)
        {
        }

        public override async Task RunAsync()
        {
            if (base.Enabled)
            {
                if (DateTime.Now >= base.NextRun)
                {
                    base.CalculateNextRun();
                    await Run();
                }
            }
        }

        private async Task Run()
        {
            await Task.Run(() =>
                LogFormatter.GenericMessage(
                    logger,
                    LogFormatterLevel.Info,
                    $"[HEARTBEAT] WRKJRNL01 Notification Worker is alive. Next heartbeat due at {base.NextRun}"));
        }
    }
}