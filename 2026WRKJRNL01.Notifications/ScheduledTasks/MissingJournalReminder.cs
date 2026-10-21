using Microsoft.Extensions.DependencyInjection;
using NLog;
using SDPBC.EmailEngine;
using SDPBC.Svc.Common.Logging;
using SDPBC.Svc.Common.TaskScheduler;
using WRKJRNL01.Data.Repository;
using WRKJRNL01.Data.Services;

namespace WRKJRNL01.Notifications.ScheduledTasks
{
    internal class MissingJournalReminder : ScheduledTaskBase
    {
        private readonly NLog.ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly IServiceProvider serviceProvider;

        public MissingJournalReminder(string taskName,IServiceProvider serviceProvider): base(taskName)
        {
            this.serviceProvider = serviceProvider;
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
            try
            {
                LogFormatter.GenericMessage(logger, LogFormatterLevel.Info,
                    $"[MissingJournalReminder] Started. Next run is at {base.NextRun}");

                var userRepo = serviceProvider.GetRequiredService<UserRepo>();
                var taskRepo = serviceProvider.GetRequiredService<TaskRepo>();
                var holidayRepo = serviceProvider.GetRequiredService<HolidayRepo>();

                var employeeResolve = serviceProvider.GetRequiredService<EmployeeResolve>();
                var googleCalendarService = serviceProvider.GetRequiredService<GoogleCalendarService>();
                var emailEngine = serviceProvider.GetRequiredService<EmailEngine>();

                //get users from application database
                var users = await userRepo.GetAllUsersAsync();

                //set date to today
                var targetDate = DateTime.Today;

                //get users that have filled out tasks today
                var employeesWithJournal = await taskRepo.GetEmployeesWithJournalEntriesAsync(targetDate);

                //get absent employees from Absence Google Calendar (if they filled it out)
                var absentEmployees = await googleCalendarService.GetAbsentEmployeeEmailsAsync(targetDate);

                int completedCount = 0;
                int missingCount = 0;
                int unresolvedCount = 0;
                int sentCount = 0;
                int failedCount = 0;

                //do not run on weekends, only run on weekdays (Monday - Friday)
                if (targetDate.DayOfWeek == DayOfWeek.Saturday || targetDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    LogFormatter.GenericMessage(logger, LogFormatterLevel.Info, $"[MissingJournalReminder] {targetDate:MM/dd/yyyy} is a weekend. Skipping reminders.");
                    return;
                } 

                //determine is district is closed by date
                var IsHoliday = await holidayRepo.Get(targetDate);

                if (IsHoliday)
                {
                    LogFormatter.GenericMessage(logger, LogFormatterLevel.Info, $"[MissingJournalReminder] {targetDate:MM/dd/yyyy} is a holiday. Skipping reminders.");
                    return;
                }

                LogFormatter.GenericMessage( logger, LogFormatterLevel.Info, $"[MissingJournalReminder] Checking {users.Count} users for {targetDate:MM/dd/yyyy}.");

                foreach (var user in users)
                {
                    try
                    {
                        var employee = await employeeResolve.GetEmployeeAsync(user.GlobalId);

                        if (employee is null)
                        {
                            unresolvedCount++;

                            LogFormatter.GenericMessage(logger, LogFormatterLevel.Warn,
                                $"[MissingJournalReminder] Could not resolve GlobalId '{user.GlobalId}'.");

                            continue;
                        }

                        if (employeesWithJournal.Contains(employee.EmplId))
                        {
                            completedCount++;

                            LogFormatter.GenericMessage( logger, LogFormatterLevel.Info, $"[MissingJournalReminder] Employee {employee.EmplId} has a journal entry. Skipping.");

                            continue;
                        }

                        if (absentEmployees.Contains(employee.Email))
                        {
                            LogFormatter.GenericMessage( logger, LogFormatterLevel.Info, $"[MissingJournalReminder] Employee {employee.EmplId} is marked absent for {targetDate:MM/dd/yyyy}. Skipping reminder.");

                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(employee.Email))
                        {
                            failedCount++;

                            LogFormatter.GenericMessage( logger, LogFormatterLevel.Warn,$"[MissingJournalReminder] Employee {employee.EmplId} has no email address. Reminder not sent.");

                            continue;
                        }

                        // No journal + not absent
                        missingCount++;

                        LogFormatter.GenericMessage(logger, LogFormatterLevel.Warn, $"[MissingJournalReminder] Employee {employee.EmplId} has no journal entry for {targetDate:MM/dd/yyyy}. Reminder required.");

                        string fromName = "Work Journal";
                        string fromEmail = "noreply-workjournal@palmbeachschools.org";
                        string subject = "Work Journal Reminder";

                        string body =
                            $"You have not completed your work journal for " +
                            $"{targetDate:MM/dd/yyyy}.<br><br>" +
                            $"Please complete your work journal.";

                        bool sent = await emailEngine.SendMail(
                            fromName,
                            fromEmail,
                            employee.Email,
                            subject,
                            body);

                        if (sent)
                        {
                            sentCount++;

                            LogFormatter.GenericMessage(
                                logger,
                                LogFormatterLevel.Info,
                                $"[MissingJournalReminder] Reminder sent for employee {employee.EmplId}.");
                        }
                        else
                        {
                            failedCount++;

                            LogFormatter.GenericMessage(
                                logger,
                                LogFormatterLevel.Warn,
                                $"[MissingJournalReminder] Reminder failed for employee {employee.EmplId}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;

                        LogFormatter.GenericMessage(
                            logger,
                            LogFormatterLevel.Error,
                            $"[MissingJournalReminder] Error processing user '{user.GlobalId}': {ex}");
                    }
                }

                LogFormatter.GenericMessage(
                    logger,
                    LogFormatterLevel.Info,
                    $"[MissingJournalReminder] Finished. " +
                    $"Completed: {completedCount}, " +
                    $"Missing: {missingCount}, " +
                    $"Sent: {sentCount}, " +
                    $"Failed: {failedCount}, " +
                    $"Unresolved: {unresolvedCount}, " +
                    $"Total: {users.Count}.");
            }
            catch (Exception ex)
            {
                LogFormatter.GenericMessage(
                    logger,
                    LogFormatterLevel.Error,
                    $"[MissingJournalReminder] Error: {ex}");
            }
        }
    }
}