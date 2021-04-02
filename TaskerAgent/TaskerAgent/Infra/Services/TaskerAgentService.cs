using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskData.OperationResults;
using TaskData.TasksGroups;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Services.RepetitiveTasksUpdaters;
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
        private readonly RepetitiveTasksUpdater mRepetitiveTasksUpdater;
        private readonly RepetitiveTasksParser mRepetitiveTasksParser;
        private readonly SummaryReporter mSummaryReporter;
        private readonly ILogger<TaskerAgentService> mLogger;

        // TODO calendar tasks + reminders.
        public TaskerAgentService(IDbRepository<ITasksGroup> TaskGroupRepository,
            ITasksGroupFactory tasksGroupFactory,
            RepetitiveTasksUpdater repetitiveTasksUpdater,
            RepetitiveTasksParser repetitiveTasksParser,
            SummaryReporter summaryReporter,
            ILogger<TaskerAgentService> logger)
        {
            mTasksGroupRepository = TaskGroupRepository ?? throw new ArgumentNullException(nameof(TaskGroupRepository));
            mTaskGroupFactory = tasksGroupFactory ?? throw new ArgumentNullException(nameof(tasksGroupFactory));
            mRepetitiveTasksUpdater = repetitiveTasksUpdater ?? throw new ArgumentNullException(nameof(repetitiveTasksUpdater));
            mRepetitiveTasksParser = repetitiveTasksParser ?? throw new ArgumentNullException(nameof(repetitiveTasksParser));
            mSummaryReporter = summaryReporter ?? throw new ArgumentNullException(nameof(summaryReporter));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SendTodaysTasksReport()
        {
            ITasksGroup tasksGroup = await GetTasksByDate(DateTime.Now).ConfigureAwait(false);

            if (tasksGroup == null)
            {
                mLogger.LogError("Could not create report for today");
                return string.Empty;
            }

            return mSummaryReporter.CreateTodaysFutureTasksReport(tasksGroup);
        }

        public async Task<string> SendThisWeekTasksReport()
        {
            IEnumerable<ITasksGroup> thisWeekGroup = await GetThisWeekGroups().ConfigureAwait(false);

            if (thisWeekGroup == null)
            {
                mLogger.LogError("Could not create report for this week");
                return string.Empty;
            }

            return mSummaryReporter.CreateThisWeekFutureTasksReport(thisWeekGroup);
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
        public async Task<string> SendDailySummary(DateTime date)
        {
            string dateString = date.ToString(TimeConsts.TimeFormat);

            ITasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(dateString).ConfigureAwait(false);

            if (tasksGroup == null)
            {
                mLogger.LogError($"Could not find task group {dateString}. Could not generate report");
                return string.Empty;
            }

            return mSummaryReporter.CreateDailySummaryReport(tasksGroup);
        }

        /// <summary>
        /// Reports tasks progress for the week of the given date.
        /// </summary>
        public async Task<string> SendWeeklySummary(DateTime dateOfTheWeek)
        {
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

            return mSummaryReporter.CreateWeeklySummaryReport(weeklyGroups);
        }
    }
}