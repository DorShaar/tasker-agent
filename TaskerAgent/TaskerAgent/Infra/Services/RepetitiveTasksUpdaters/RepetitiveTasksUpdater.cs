using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskData.OperationResults;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.App.RepetitiveTasks;
using TaskerAgent.App.Services.RepetitiveTasksUpdaters;
using TaskerAgent.App.TasksProducers;
using TaskerAgent.Domain.RepetitiveTasks;
using TaskerAgent.Domain.RepetitiveTasks.TasksClusters;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.Email;
using Triangle.Time;

namespace TaskerAgent.Infra.Services.RepetitiveTasksUpdaters
{
    public class RepetitiveTasksUpdater : IRepetitiveTasksUpdater
    {
        private const string EmailMessageNewLine = "\r\n";

        private readonly IDbRepository<ITasksGroup> mTasksGroupRepository;
        private readonly ITasksGroupFactory mTaskGroupFactory;
        private readonly ITasksProducerFactory mTasksProducerFactory;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<RepetitiveTasksUpdater> mLogger;

        public RepetitiveTasksUpdater(IDbRepository<ITasksGroup> tasksGroupRepository,
            ITasksGroupFactory taskGroupFactory,
            ITasksProducerFactory tasksProducerFactory,
            IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
            ILogger<RepetitiveTasksUpdater> logger)
        {
            mTasksGroupRepository = tasksGroupRepository ?? throw new ArgumentNullException(nameof(tasksGroupRepository));
            mTaskGroupFactory = taskGroupFactory ?? throw new ArgumentNullException(nameof(taskGroupFactory));
            mTasksProducerFactory = tasksProducerFactory ?? throw new ArgumentNullException(nameof(tasksProducerFactory));
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Update(ITasksGroup tasksGroupToUpdateAccordingly)
        {
            TasksCluster tasksCluster = TasksCluster.SplitTaskGroupByFrequency(tasksGroupToUpdateAccordingly);

            foreach (DateTime date in GetNextDaysDates(mTaskerAgentOptions.CurrentValue.DaysToKeepForward))
            {
                string groupName = date.ToString(TimeConsts.TimeFormat);

                ITasksGroup taskGroup =
                    await mTasksGroupRepository.FindAsync(groupName).ConfigureAwait(false) ??
                    await AddNewGroup(groupName).ConfigureAwait(false);

                await UpdateDailyTasks(taskGroup, tasksCluster.DailyTasks).ConfigureAwait(false);
                await UpdateWeeklyTasks(taskGroup, tasksCluster.WeeklyTasks, date).ConfigureAwait(false);
                await UpdateMonthlyTasks(taskGroup, tasksCluster.MonthlyTasks, date).ConfigureAwait(false);
            }
        }

        private IEnumerable<DateTime> GetNextDaysDates(int nextDays)
        {
            for (int i = 0; i < nextDays; ++i)
            {
                yield return DateTime.Now.AddDays(1 + i);
            }
        }

        private async Task<ITasksGroup> AddNewGroup(string groupName)
        {
            OperationResult<ITasksGroup> taskGroupResult = mTaskGroupFactory.CreateGroup(groupName);
            if (!taskGroupResult.Success)
            {
                mLogger.LogError($"Could not create group {groupName}");
                return null;
            }

            await mTasksGroupRepository.AddAsync(taskGroupResult.Value).ConfigureAwait(false);
            return taskGroupResult.Value;
        }

        private async Task UpdateDailyTasks(ITasksGroup currentTaskGroup, IEnumerable<IWorkTask> tasksToUpdateAccordingly)
        {
            foreach (IWorkTask taskToUpdateAccordingly in tasksToUpdateAccordingly)
            {
                if (!(taskToUpdateAccordingly is DailyRepetitiveMeasureableTask repititiveTaskToUpdateAccordingly))
                    continue;

                UpdateGroup(currentTaskGroup, repititiveTaskToUpdateAccordingly);
            }

            await mTasksGroupRepository.AddOrUpdateAsync(currentTaskGroup).ConfigureAwait(false);
        }

        private async Task UpdateWeeklyTasks(ITasksGroup currentTaskGroup, IEnumerable<IWorkTask> tasksToUpdateAccordingly,
            DateTime date)
        {
            foreach (IWorkTask taskToUpdateAccordingly in tasksToUpdateAccordingly)
            {
                if (!(taskToUpdateAccordingly is WeeklyRepetitiveMeasureableTask repititiveTaskToUpdateAccordingly))
                    continue;

                if (repititiveTaskToUpdateAccordingly.IsDayIsOneOfWeeklyOccurrence(date.DayOfWeek))
                {
                    UpdateGroup(currentTaskGroup, repititiveTaskToUpdateAccordingly);
                }
            }

            await mTasksGroupRepository.AddOrUpdateAsync(currentTaskGroup).ConfigureAwait(false);
        }

        private async Task UpdateMonthlyTasks(ITasksGroup currentTaskGroup, IEnumerable<IWorkTask> tasksToUpdateAccordingly,
            DateTime date)
        {
            foreach (IWorkTask taskToUpdateAccordingly in tasksToUpdateAccordingly)
            {
                if (!(taskToUpdateAccordingly is MonthlyRepetitiveMeasureableTask repititiveTaskToUpdateAccordingly))
                    continue;

                if (repititiveTaskToUpdateAccordingly.IsDayIsOneOfMonthlyOccurrence(date.Day))
                {
                    UpdateGroup(currentTaskGroup, repititiveTaskToUpdateAccordingly);
                }
            }

            await mTasksGroupRepository.AddOrUpdateAsync(currentTaskGroup).ConfigureAwait(false);
        }

        private void UpdateGroup(ITasksGroup currentTaskGroup, IRepetitiveMeasureableTask repititiveTaskToUpdateAccordingly)
        {
            bool isNewTask = true;

            foreach (IWorkTask currentTask in currentTaskGroup.GetAllTasks())
            {
                if (!(currentTask is IRepetitiveMeasureableTask currentRepititiveTask))
                    continue;

                if (repititiveTaskToUpdateAccordingly.Description.Equals(currentTask.Description, StringComparison.OrdinalIgnoreCase))
                {
                    UpdateCurrentTask(currentRepititiveTask, repititiveTaskToUpdateAccordingly);
                    isNewTask = false;
                }
            }

            if (isNewTask)
                CreateNewTask(currentTaskGroup, repititiveTaskToUpdateAccordingly);
        }

        private void UpdateCurrentTask(IRepetitiveMeasureableTask currentTask,
            IRepetitiveMeasureableTask taskToUpdateAccordingly)
        {
            if (currentTask.Expected != taskToUpdateAccordingly.Expected ||
                currentTask.MeasureType != taskToUpdateAccordingly.MeasureType)
            {
                currentTask.Expected = taskToUpdateAccordingly.Expected;
                currentTask.MeasureType = taskToUpdateAccordingly.MeasureType;
            }
        }

        private void CreateNewTask(ITasksGroup currentTaskGroup, IRepetitiveMeasureableTask taskToUpdateAccordingly)
        {
            IWorkTaskProducer taskProducer = CreateTaskProducer(taskToUpdateAccordingly);

            if (taskProducer == null)
            {
                mLogger.LogError($"Unexpected task type given {nameof(taskToUpdateAccordingly)}");
                return;
            }

            OperationResult<IWorkTask> createTaskResult =
                mTaskGroupFactory.CreateTask(currentTaskGroup, taskToUpdateAccordingly.Description, taskProducer);

            if (!createTaskResult.Success)
                mLogger.LogError($"Could not create task {taskToUpdateAccordingly.Description}");
        }

        private IWorkTaskProducer CreateTaskProducer(IRepetitiveMeasureableTask taskToUpdateAccordingly)
        {
            if (taskToUpdateAccordingly is DailyRepetitiveMeasureableTask dailyTask)
            {
                return mTasksProducerFactory.CreateDailyProducer(
                    dailyTask.Frequency, dailyTask.MeasureType, dailyTask.Expected, score: 1);
            }

            if (taskToUpdateAccordingly is WeeklyRepetitiveMeasureableTask weeklyTask)
            {
                return mTasksProducerFactory.CreateWeeklyProducer(
                    weeklyTask.Frequency, weeklyTask.MeasureType, weeklyTask.OccurrenceDays, weeklyTask.Expected, score: 1);
            }

            if (taskToUpdateAccordingly is MonthlyRepetitiveMeasureableTask monthlyTask)
            {
                return mTasksProducerFactory.CreateMonthlyProducer(
                    monthlyTask.Frequency, monthlyTask.MeasureType, monthlyTask.DaysOfMonth, monthlyTask.Expected, score: 1);
            }

            return null;
        }

        public async Task UpdateGroupByMessage(MessageInfo message)
        {
            try
            {
                string[] stringTasks = message.Body.Split(EmailMessageNewLine);
                string date = stringTasks[1].Split("- ")[1].Replace(":", string.Empty);
                ITasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(date).ConfigureAwait(false);

                if (tasksGroup == null)
                {
                    mLogger.LogError("Failed to update according to given message");
                    return;
                }

                IEnumerable<IWorkTask> tasks = tasksGroup.GetAllTasks();
                foreach (string messagePart in stringTasks.Skip(2))
                {
                    if (string.IsNullOrWhiteSpace(messagePart))
                        continue;

                    string[] subMessageParts = messagePart.Split(".");
                    string description = subMessageParts[0];
                    IWorkTask task = tasks.FirstOrDefault(task => task.Description.Equals(description, StringComparison.OrdinalIgnoreCase));

                    if (task == null || !(task is GeneralRepetitiveMeasureableTask repetitiveMeasureableTask))
                    {
                        mLogger.LogWarning($"Could not find task {description}");
                        continue;
                    }

                    string actualString = subMessageParts[2]
                        .Replace(".", string.Empty)
                        .Replace("actual:", string.Empty, StringComparison.OrdinalIgnoreCase)
                        .Trim();
                    repetitiveMeasureableTask.Actual = Convert.ToInt32(actualString);
                }

                await mTasksGroupRepository.AddOrUpdateAsync(tasksGroup).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Could not update tasks by message id {message.Id}");
            }
        }
    }
}