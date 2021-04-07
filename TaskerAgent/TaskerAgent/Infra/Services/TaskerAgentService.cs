using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskData.OperationResults;
using TaskData.TasksGroups;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.App.Services.Email;
using TaskerAgent.App.Services.RepetitiveTasksUpdaters;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Services.Email;
using TaskerAgent.Infra.Services.SummaryReporters;
using TaskerAgent.Infra.Services.TasksParser;
using Triangle.Time;

namespace TaskerAgent.Infra.Services
{
    public class TaskerAgentService
    {
        private const string FromConfigGroupName = "from-config";

        private readonly IDbRepository<ITasksGroup> mTasksGroupRepository;
        private readonly ITasksGroupFactory mTaskGroupFactory;
        private readonly IRepetitiveTasksUpdater mRepetitiveTasksUpdater;
        private readonly IEmailService mEmailService;
        private readonly RepetitiveTasksParser mRepetitiveTasksParser;
        private readonly SummaryReporter mSummaryReporter;
        private readonly ILogger<TaskerAgentService> mLogger;

        // TODO calendar tasks + reminders.
        public TaskerAgentService(IDbRepository<ITasksGroup> TaskGroupRepository,
            ITasksGroupFactory tasksGroupFactory,
            IRepetitiveTasksUpdater repetitiveTasksUpdater,
            IEmailService emailService,
            RepetitiveTasksParser repetitiveTasksParser,
            SummaryReporter summaryReporter,
            ILogger<TaskerAgentService> logger)
        {
            mTasksGroupRepository = TaskGroupRepository ?? throw new ArgumentNullException(nameof(TaskGroupRepository));
            mTaskGroupFactory = tasksGroupFactory ?? throw new ArgumentNullException(nameof(tasksGroupFactory));
            mRepetitiveTasksUpdater = repetitiveTasksUpdater ?? throw new ArgumentNullException(nameof(repetitiveTasksUpdater));
            mEmailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            mRepetitiveTasksParser = repetitiveTasksParser ?? throw new ArgumentNullException(nameof(repetitiveTasksParser));
            mSummaryReporter = summaryReporter ?? throw new ArgumentNullException(nameof(summaryReporter));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            mEmailService.Connect();
        }

        public async Task SendTodaysTasksReport()
        {
            ITasksGroup tasksGroup = await GetTasksByDate(DateTime.Now).ConfigureAwait(false);

            if (tasksGroup == null)
                mLogger.LogError("Could not create report for today");

            string todaysFutureTasksReport = mSummaryReporter.CreateTodaysFutureTasksReport(tasksGroup);

            await mEmailService.SendMessage("Today's tasks", todaysFutureTasksReport).ConfigureAwait(false);
        }

        public async Task SendThisWeekTasksReport()
        {
            IEnumerable<ITasksGroup> thisWeekGroup = await GetThisWeekGroups().ConfigureAwait(false);

            if (thisWeekGroup == null)
                mLogger.LogError("Could not create report for this week");

            string thisWeekFutureTasksReport = mSummaryReporter.CreateThisWeekFutureTasksReport(thisWeekGroup);

            await mEmailService.SendMessage("Today's tasks", thisWeekFutureTasksReport).ConfigureAwait(false);
        }

        private async Task<IEnumerable<ITasksGroup>> GetThisWeekGroups()
        {
            List<ITasksGroup> thisWeekGroups = new List<ITasksGroup>();

            foreach (DateTime date in GetDatesOfWeek())
            {
                ITasksGroup tasksGroup = await GetTasksByDate(date).ConfigureAwait(false);

                if (tasksGroup == null)
                {
                    mLogger.LogWarning($"Could not find task group of date {date.ToString(TimeConsts.TimeFormat)}");
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
                mLogger.LogWarning($"Could not find task group {stringDate}");
                return null;
            }

            return tasksGroup;
        }

        private IEnumerable<DateTime> GetDatesOfWeek(DateTime date = default)
        {
            if (date == default)
                date = DateTime.Now;

            DateTime startOfWeekDate = date.StartOfWeek();

            for (int i = 0; i < 7; ++i)
            {
                yield return startOfWeekDate.AddDays(i);
            }
        }

        public async Task UpdateRepetitiveTasks()
        {
            mLogger.LogDebug("Updating repetitive tasks");

            ITasksGroup tasksFromConfigGroup = ReadRepetitiveTasksFromInputFile();

            if (tasksFromConfigGroup == null)
            {
                mLogger.LogError("Could not update repetitive tasks");
                return;
            }

            await mRepetitiveTasksUpdater.Update(tasksFromConfigGroup).ConfigureAwait(false);
        }

        private ITasksGroup ReadRepetitiveTasksFromInputFile()
        {
            OperationResult<ITasksGroup> tasksFromConfigGroupCreationResult = mTaskGroupFactory.CreateGroup(FromConfigGroupName);
            if (!tasksFromConfigGroupCreationResult.Success)
            {
                mLogger.LogError($"Could not create group {FromConfigGroupName}");
                return null;
            }

            ITasksGroup tasksFromConfigGroup = tasksFromConfigGroupCreationResult.Value;
            mRepetitiveTasksParser.ParseIntoGroup(tasksFromConfigGroup);
            return tasksFromConfigGroup;
        }

        /// <summary>
        /// Reports tasks progress for the day of the given date.
        /// </summary>
        /// <param name="date"></param>
        public async Task SendDailySummary(DateTime date)
        {
            mLogger.LogInformation("Sending daily summary");

            string dateString = date.ToString(TimeConsts.TimeFormat);

            ITasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(dateString).ConfigureAwait(false);

            if (tasksGroup == null)
                mLogger.LogError($"Could not find task group {dateString}. Could not generate report");

            string dailySummaryReport = mSummaryReporter.CreateDailySummaryReport(tasksGroup);
            await mEmailService.SendMessage("Daily Summary Report", dailySummaryReport).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports tasks progress for the week of the given date.
        /// </summary>
        public async Task SendWeeklySummary(DateTime dateOfTheWeek)
        {
            mLogger.LogInformation("Sending weekly summary");

            List<ITasksGroup> weeklyGroups = new List<ITasksGroup>(7);

            foreach (DateTime date in GetDatesOfWeek(dateOfTheWeek))
            {
                string dateString = date.ToString(TimeConsts.TimeFormat);

                ITasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(dateString).ConfigureAwait(false);

                if (tasksGroup == null)
                {
                    mLogger.LogError($"Could not find task group {dateString}. Report may be partial");
                    continue;
                }

                weeklyGroups.Add(tasksGroup);
            }

            string weeklySummaryReport =  mSummaryReporter.CreateWeeklySummaryReport(weeklyGroups);
            await mEmailService.SendMessage("Weekly Summary Report", weeklySummaryReport).ConfigureAwait(false);
        }

        public async Task<IEnumerable<DateTime>> CheckForUpdates()
        {
            mLogger.LogInformation("Checking for updates");

            IEnumerable<MessageInfo> messages = await mEmailService.ReadMessages().ConfigureAwait(false);

            if (messages?.Any() != true)
            {
                mLogger.LogDebug("No new messages found. Nothing to updated");
                return new DateTime[0];
            }

            foreach(MessageInfo message in messages)
            {
                await mRepetitiveTasksUpdater.UpdateGroupByMessage(message).ConfigureAwait(false);
                await mEmailService.MarkMessageAsRead(message.Id).ConfigureAwait(false);
            }
        }
    }
}