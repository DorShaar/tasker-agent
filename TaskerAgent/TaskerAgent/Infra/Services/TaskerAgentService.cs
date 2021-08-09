using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.App.Services.Calendar;
using TaskerAgent.App.Services.Email;
using TaskerAgent.App.Services.TasksUpdaters;
using TaskerAgent.Domain.Email;
using TaskerAgent.Domain.TaskerDateTime;
using TaskerAgent.Domain.TaskGroup;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.SummaryReporters;
using Triangle.Time;

namespace TaskerAgent.Infra.Services
{
    public class TaskerAgentService
    {
        private readonly IDbRepository<DailyTasksGroup> mTasksGroupRepository;
        private readonly ITasksSynchronizer mTasksSynchronizer;
        private readonly IEmailService mEmailService;
        private readonly SummaryReporter mSummaryReporter;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerOptions;
        private readonly ILogger<TaskerAgentService> mLogger;

        public bool IsAgentReady { get; }

        public TaskerAgentService(IDbRepository<DailyTasksGroup> TaskGroupRepository,
            ITasksSynchronizer tasksSynchronizer,
            IEmailService emailService,
            SummaryReporter summaryReporter,
            IOptionsMonitor<TaskerAgentConfiguration> taskerOptions,
            ILogger<TaskerAgentService> logger)
        {
            mTasksGroupRepository = TaskGroupRepository ?? throw new ArgumentNullException(nameof(TaskGroupRepository));
            mTasksSynchronizer = tasksSynchronizer ?? throw new ArgumentNullException(nameof(tasksSynchronizer));
            mEmailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            mSummaryReporter = summaryReporter ?? throw new ArgumentNullException(nameof(summaryReporter));
            mTaskerOptions = taskerOptions ?? throw new ArgumentNullException(nameof(taskerOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            mEmailService.Connect().Wait();
            IsAgentReady = true;
        }

        public async Task<bool> SendTodaysFutureTasksReport()
        {
            ITasksGroup tasksGroup = await GetTasksByDate(DateTime.Now).ConfigureAwait(false);

            if (tasksGroup == null)
                mLogger.LogError("Could not create report for today");

            string todaysFutureTasksReport = mSummaryReporter.CreateTodaysFutureTasksReport(tasksGroup);

            return await mEmailService.SendMessage("Today's tasks", todaysFutureTasksReport).ConfigureAwait(false);
        }

        public async Task<bool> SendThisWeekFutureTasksReport()
        {
            IEnumerable<ITasksGroup> thisWeekGroup = await GetThisWeekGroups().ConfigureAwait(false);

            if (thisWeekGroup == null)
                mLogger.LogError("Could not create report for this week");

            string thisWeekFutureTasksReport = mSummaryReporter.CreateThisWeekFutureTasksReport(thisWeekGroup);

            return await mEmailService.SendMessage("Today's tasks", thisWeekFutureTasksReport).ConfigureAwait(false);
        }

        private async Task<IEnumerable<ITasksGroup>> GetThisWeekGroups()
        {
            List<ITasksGroup> thisWeekGroups = new List<ITasksGroup>();

            foreach (DateTime date in DateTimeUtilities.GetDatesOfWeek())
            {
                ITasksGroup tasksGroup = await GetTasksByDate(date).ConfigureAwait(false);

                if (tasksGroup == null)
                {
                    mLogger.LogWarning($"Could not find tasks group of date {date.ToString(TimeConsts.TimeFormat)}");
                    continue;
                }

                thisWeekGroups.Add(tasksGroup);
            }

            return thisWeekGroups;
        }

        private async Task<ITasksGroup> GetTasksByDate(DateTime date)
        {
            string stringDate = date.ToString(TimeConsts.TimeFormat);

            ITasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(stringDate).ConfigureAwait(false);

            if (tasksGroup == null)
            {
                mLogger.LogWarning($"Could not find tasks group {stringDate}");
                return null;
            }

            return tasksGroup;
        }

        public async Task SynchronizeCalendarTasks()
        {
            await mTasksSynchronizer.Synchronize().ConfigureAwait(false);
        }

        /// <summary>
        /// Reports tasks progress for the day of the given date.
        /// </summary>
        /// <param name="date"></param>
        public async Task<bool> SendDailySummary(DateTime date)
        {
            mLogger.LogInformation($"Sending daily summary for {date.ToString(TimeConsts.TimeFormat)}");

            string dateString = date.ToString(TimeConsts.TimeFormat);

            ITasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(dateString).ConfigureAwait(false);

            if (tasksGroup == null)
            {
                mLogger.LogError($"Could not find tasks group {dateString}. Could not generate report");
                return false;
            }

            string dailySummaryReport = mSummaryReporter.CreateDailySummaryReport(tasksGroup);
            return await mEmailService.SendMessage("Daily Summary Report", dailySummaryReport).ConfigureAwait(false);
        }

        /// <summary>
        /// Go over all the previous "DaysToKeepForward" (found in configruation) and checks if already reported.
        /// Sending one message with all the missing unreported dates.
        /// </summary>
        public async Task<bool> SendMissingReportsMessage()
        {
            List<DailyTasksGroup> notReportedDailyTasksGroup = await GetNotReportedDailyTasksGroups().ConfigureAwait(false);

            string missingDailyReportMessageAlart = mSummaryReporter.CreateMissingDailyReportMessageAlart(notReportedDailyTasksGroup.Select(group => group.Name));
            return await mEmailService.SendMessage("Missing Daily Report", missingDailyReportMessageAlart).ConfigureAwait(false);
        }

        private async Task<List<DailyTasksGroup>> GetNotReportedDailyTasksGroups()
        {
            List<DailyTasksGroup> notReportedDailyTasksGroup = new List<DailyTasksGroup>();

            foreach (DateTime date in DateTimeUtilities.GetPreviousDaysDates(mTaskerOptions.CurrentValue.DaysToKeepForward))
            {
                string stringDate = date.ToString(TimeConsts.TimeFormat);

                DailyTasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(stringDate).ConfigureAwait(false);

                if (tasksGroup == null)
                {
                    mLogger.LogInformation($"Group for date {date.ToString(TimeConsts.TimeFormat)} not found");
                    continue;
                }

                if (tasksGroup?.IsAlreadyReported == false)
                {
                    mLogger.LogInformation($"Group {tasksGroup.Name} was not reported yet by the user");
                    notReportedDailyTasksGroup.Add(tasksGroup);
                }
            }

            return notReportedDailyTasksGroup;
        }

        public async Task SignalDateGivenFeedbackByUser(DateTime date)
        {
            string dateString = date.ToString(TimeConsts.TimeFormat);

            DailyTasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(dateString).ConfigureAwait(false);

            if (tasksGroup == null)
            {
                mLogger.LogError($"Group for date {date.ToString(TimeConsts.TimeFormat)} not found");
                return;
            }

            tasksGroup.SetReported();
            mLogger.LogInformation($"Daily Group {tasksGroup.Name} set as reported");

            await mTasksGroupRepository.AddOrUpdateAsync(tasksGroup).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports tasks progress for the week of the given date.
        /// </summary>
        public async Task<bool> SendWeeklySummary(DateTime dateOfTheWeek)
        {
            mLogger.LogInformation("Sending weekly summary");

            List<ITasksGroup> weeklyGroups = new List<ITasksGroup>(7);

            foreach (DateTime date in DateTimeUtilities.GetDatesOfWeek(dateOfTheWeek))
            {
                string dateString = date.ToString(TimeConsts.TimeFormat);

                ITasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(dateString).ConfigureAwait(false);

                if (tasksGroup == null)
                {
                    mLogger.LogError($"Could not find tasks group {dateString}. Report may be partial");
                    continue;
                }

                weeklyGroups.Add(tasksGroup);
            }

            string weeklySummaryReport = mSummaryReporter.CreateWeeklySummaryReport(weeklyGroups);
            return await mEmailService.SendMessage("Weekly Summary Report", weeklySummaryReport).ConfigureAwait(false);
        }
    }
}