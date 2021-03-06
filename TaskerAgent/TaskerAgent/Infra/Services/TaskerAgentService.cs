using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskData.OperationResults;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.TasksParser;
using Triangle.Time;

namespace TaskerAgent.Infra.Services
{
    public class TaskerAgentService
    {
        private const string FromConfigGroupName = "from-config";

        private readonly IDbRepository<ITasksGroup> mTasksGroupRepository;
        private readonly ITasksGroupFactory mTaskGroupFactory;
        private readonly RepetitiveTasksParser mRepetitiveTasksParser;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<TaskerAgentService> mLogger;

        // TODO every morning writes all the todays tasks.
        // TODO every Sunday writes all this week tasks.

        // TODO calendar tasks + reminders.
        public TaskerAgentService(IDbRepository<ITasksGroup> TaskGroupRepository,
            ITasksGroupFactory tasksGroupFactory,
            RepetitiveTasksParser repetitiveTasksParser,
            IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
            ILogger<TaskerAgentService> logger)
        {
            mTasksGroupRepository = TaskGroupRepository ?? throw new ArgumentNullException(nameof(TaskGroupRepository));
            mTaskGroupFactory = tasksGroupFactory ?? throw new ArgumentNullException(nameof(tasksGroupFactory));
            mRepetitiveTasksParser = repetitiveTasksParser ?? throw new ArgumentNullException(nameof(repetitiveTasksParser));
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<IWorkTask>> GetTodaysTasks()
        {
            string todayDate = DateTime.Now.ToString(TimeConsts.TimeFormat);

            ITasksGroup todaysTasksGroup = await mTasksGroupRepository.FindAsync(todayDate).ConfigureAwait(false);

            if (todaysTasksGroup == null)
                return null;

            return todaysTasksGroup.GetAllTasks();
        }

        public async Task<IEnumerable<IWorkTask>> GetThisWeekTasks()
        {
            List<IWorkTask> thisWeekTasks = new List<IWorkTask>();

            foreach (DateTime date in GetThisWeekDates())
            {
                ITasksGroup taskGroup = await mTasksGroupRepository.FindAsync(date.ToString(TimeConsts.TimeFormat)).ConfigureAwait(false);

                if (taskGroup == null)
                    continue;

                thisWeekTasks.AddRange(taskGroup.GetAllTasks());
            }

            return thisWeekTasks;
        }

        private IEnumerable<DateTime> GetThisWeekDates()
        {
            DateTime startOfWeekDate = DateTime.Now.StartOfWeek();

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

            // TODO further implement.
            if (await WasUpdatePerformed(tasksFromConfigGroup).ConfigureAwait(false))
                await Update(tasksFromConfigGroup).ConfigureAwait(false);
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

        // TODO Create summary with score on Saturday.
        //public async Task<string> SendDailySummary() // tODO
        //{

        //}

        //public async Task<string> SendWeeklySummary() // tODO
        //{

        //}

        //public async Task<string> SendMonthlySummary() // tODO
        //{

        //}
    }
}