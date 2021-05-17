using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskData.OperationResults;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskData.WorkTasks.Producers;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.App.RepetitiveTasks;
using TaskerAgent.App.Services.RepetitiveTasksUpdaters;
using TaskerAgent.App.TasksProducers;
using TaskerAgent.Domain.Email;
using TaskerAgent.Domain.RepetitiveTasks;
using TaskerAgent.Domain.RepetitiveTasks.TasksClusters;
using TaskerAgent.Domain.TaskGroup;
using TaskerAgent.Infra.Consts;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.TaskerDateTime;
using Triangle.Time;

namespace TaskerAgent.Infra.Services.RepetitiveTasksUpdaters
{
    public class RepetitiveTasksUpdater : IRepetitiveTasksUpdater
    {
        private const string EmailFeedbackSubject = "Re: Today's tasks";

        private readonly IDbRepository<DailyTasksGroup> mTasksGroupRepository;
        private readonly ITasksGroupFactory mTaskGroupFactory;
        private readonly ITasksProducerFactory mTasksProducerFactory;
        private readonly ITasksGroupProducer mTasksGroupProducer;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<RepetitiveTasksUpdater> mLogger;

        public RepetitiveTasksUpdater(IDbRepository<DailyTasksGroup> tasksGroupRepository,
            ITasksGroupFactory taskGroupFactory,
            ITasksProducerFactory tasksProducerFactory,
            ITasksGroupProducer tasksGroupProducer,
            IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
            ILogger<RepetitiveTasksUpdater> logger)
        {
            mTasksGroupRepository = tasksGroupRepository ?? throw new ArgumentNullException(nameof(tasksGroupRepository));
            mTaskGroupFactory = taskGroupFactory ?? throw new ArgumentNullException(nameof(taskGroupFactory));
            mTasksProducerFactory = tasksProducerFactory ?? throw new ArgumentNullException(nameof(tasksProducerFactory));
            mTasksGroupProducer = tasksGroupProducer ?? throw new ArgumentNullException(nameof(tasksGroupProducer));
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Update(ITasksGroup tasksGroupToUpdateAccordingly)
        {
            TasksCluster tasksCluster = TasksCluster.SplitTaskGroupByFrequency(tasksGroupToUpdateAccordingly);

            foreach (DateTime date in DateTimeUtilities.GetNextDaysDates(mTaskerAgentOptions.CurrentValue.DaysToKeepForward))
            {
                string groupName = date.ToString(TimeConsts.TimeFormat);

                DailyTasksGroup taskGroup =
                    await mTasksGroupRepository.FindAsync(groupName).ConfigureAwait(false) ??
                    await AddNewGroup(groupName).ConfigureAwait(false);

                await UpdateDailyTasks(taskGroup, tasksCluster.DailyTasks).ConfigureAwait(false);
                await UpdateWeeklyTasks(taskGroup, tasksCluster.WeeklyTasks, date).ConfigureAwait(false);
                await UpdateMonthlyTasks(taskGroup, tasksCluster.MonthlyTasks, date).ConfigureAwait(false);
            }
        }

        private async Task<DailyTasksGroup> AddNewGroup(string groupName)
        {
            OperationResult<ITasksGroup> taskGroupResult = mTaskGroupFactory.CreateGroup(groupName, mTasksGroupProducer);
            if (!taskGroupResult.Success)
            {
                mLogger.LogError($"Could not create group {groupName}");
                return null;
            }

            if (taskGroupResult.Value is not DailyTasksGroup dailyTasksGroup)
            {
                mLogger.LogError($"Tasks group is not of type {nameof(DailyTasksGroup)}");
                return null;
            }

            await mTasksGroupRepository.AddAsync(dailyTasksGroup).ConfigureAwait(false);
            return dailyTasksGroup;
        }

        private async Task UpdateDailyTasks(DailyTasksGroup currentTaskGroup,
            IEnumerable<DailyRepetitiveMeasureableTask> tasksToUpdateAccordingly)
        {
            foreach (DailyRepetitiveMeasureableTask taskToUpdateAccordingly in tasksToUpdateAccordingly)
            {
                UpdateGroup(currentTaskGroup, taskToUpdateAccordingly);
            }

            await mTasksGroupRepository.AddOrUpdateAsync(currentTaskGroup).ConfigureAwait(false);
        }

        private async Task UpdateWeeklyTasks(DailyTasksGroup currentTaskGroup,
            IEnumerable<WeeklyRepetitiveMeasureableTask> tasksToUpdateAccordingly,
            DateTime date)
        {
            foreach (WeeklyRepetitiveMeasureableTask taskToUpdateAccordingly in tasksToUpdateAccordingly)
            {
                if (taskToUpdateAccordingly.IsDayIsOneOfWeeklyOccurrence(date.DayOfWeek))
                {
                    UpdateGroup(currentTaskGroup, taskToUpdateAccordingly);
                }
            }

            await mTasksGroupRepository.AddOrUpdateAsync(currentTaskGroup).ConfigureAwait(false);
        }

        private async Task UpdateMonthlyTasks(DailyTasksGroup currentTaskGroup,
            IEnumerable<MonthlyRepetitiveMeasureableTask> tasksToUpdateAccordingly,
            DateTime date)
        {
            foreach (MonthlyRepetitiveMeasureableTask taskToUpdateAccordingly in tasksToUpdateAccordingly)
            {
                if (taskToUpdateAccordingly.IsDayIsOneOfMonthlyOccurrence(date.Day))
                {
                    UpdateGroup(currentTaskGroup, taskToUpdateAccordingly);
                }
            }

            await mTasksGroupRepository.AddOrUpdateAsync(currentTaskGroup).ConfigureAwait(false);
        }

        private void UpdateGroup(ITasksGroup currentTaskGroup, IRepetitiveMeasureableTask repititiveTaskToUpdateAccordingly)
        {
            bool isNewTask = true;

            foreach (IWorkTask currentTask in currentTaskGroup.GetAllTasks())
            {
                if (currentTask is not IRepetitiveMeasureableTask currentRepititiveTask)
                    continue;

                if (repititiveTaskToUpdateAccordingly.Description.Equals(currentTask.Description, StringComparison.OrdinalIgnoreCase))
                {
                    isNewTask = false;
                    UpdateCurrentTaskIfNeeded(currentRepititiveTask, repititiveTaskToUpdateAccordingly);
                }
            }

            if (isNewTask)
                CreateNewTask(currentTaskGroup, repititiveTaskToUpdateAccordingly);
        }

        private void UpdateCurrentTaskIfNeeded(IRepetitiveMeasureableTask currentTask,
            IRepetitiveMeasureableTask taskToUpdateAccordingly)
        {
            if (IsTaskShouldBeUpdated(currentTask, taskToUpdateAccordingly))
            {
                currentTask.Expected = taskToUpdateAccordingly.Expected;
                currentTask.MeasureType = taskToUpdateAccordingly.MeasureType;
            }

            if (currentTask is MonthlyRepetitiveMeasureableTask currentMonthlyTask &&
                taskToUpdateAccordingly is MonthlyRepetitiveMeasureableTask monthlyTaskToUpdateAccordingly)
            {
                UpdateCurrentMonthlyTaskIfNeeded(currentMonthlyTask, monthlyTaskToUpdateAccordingly);
            }
        }

        private static bool IsTaskShouldBeUpdated(IRepetitiveMeasureableTask currentTask,
            IRepetitiveMeasureableTask taskToUpdateAccordingly)
        {
            return currentTask.Expected != taskToUpdateAccordingly.Expected ||
                currentTask.MeasureType != taskToUpdateAccordingly.MeasureType;
        }

        private void UpdateCurrentMonthlyTaskIfNeeded(MonthlyRepetitiveMeasureableTask currentTask,
            MonthlyRepetitiveMeasureableTask taskToUpdateAccordingly)
        {
            List<int> firstNotSecond = currentTask.DaysOfMonth.Except(taskToUpdateAccordingly.DaysOfMonth).ToList();

            if (firstNotSecond.Count == 0)
            {
                List<int> secondNotFirst = taskToUpdateAccordingly.DaysOfMonth.Except(currentTask.DaysOfMonth).ToList();
                if (secondNotFirst.Count == 0)
                {
                    return;
                }
            }

            mLogger.LogDebug($"Updating Days of month for task {currentTask.Description}");
            currentTask.DaysOfMonth.Clear();
            currentTask.DaysOfMonth.AddRange(taskToUpdateAccordingly.DaysOfMonth);
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
                    dailyTask.MeasureType, dailyTask.Expected, score: 1);
            }

            if (taskToUpdateAccordingly is WeeklyRepetitiveMeasureableTask weeklyTask)
            {
                return mTasksProducerFactory.CreateWeeklyProducer(
                    weeklyTask.MeasureType, weeklyTask.OccurrenceDays, weeklyTask.Expected, score: 1);
            }

            if (taskToUpdateAccordingly is MonthlyRepetitiveMeasureableTask monthlyTask)
            {
                return mTasksProducerFactory.CreateMonthlyProducer(
                    monthlyTask.MeasureType, monthlyTask.DaysOfMonth, monthlyTask.Expected, score: 1);
            }

            return null;
        }

        public async Task<bool> UpdateGroupByMessage(MessageInfo message)
        {
            try
            {
                if (!message.Subject.Equals(EmailFeedbackSubject, StringComparison.OrdinalIgnoreCase))
                {
                    mLogger.LogDebug("That message is not user's feedback");
                    return false;
                }

                string[] stringTasks = message.Body.Split(AppConsts.EmailMessageNewLine);
                string date = stringTasks[1].Split("- ")[1].Replace(":", string.Empty);
                DailyTasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(date).ConfigureAwait(false);

                if (tasksGroup == null)
                {
                    mLogger.LogError("Failed to update according to given message");
                    return false;
                }

                IEnumerable<IWorkTask> tasks = tasksGroup.GetAllTasks();
                foreach (string messagePart in stringTasks.Skip(2))
                {
                    UpdateTask(tasks, messagePart);
                }

                await mTasksGroupRepository.AddOrUpdateAsync(tasksGroup).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Could not update tasks by message id {message.Id}");
                return false;
            }
        }

        private void UpdateTask(IEnumerable<IWorkTask> tasks, string messagePart)
        {
            if (string.IsNullOrWhiteSpace(messagePart))
                return;

            string[] subMessageParts = messagePart.Split(".");
            string description = subMessageParts[0];
            IWorkTask task = tasks.FirstOrDefault(task => task.Description.Equals(description, StringComparison.OrdinalIgnoreCase));

            if (task == null || task is not GeneralRepetitiveMeasureableTask repetitiveMeasureableTask)
            {
                mLogger.LogWarning($"Could not find task {description}");
                return;
            }

            string actualString = subMessageParts[2]
                .Replace(".", string.Empty)
                .Replace("actual:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            repetitiveMeasureableTask.Actual = Convert.ToInt32(actualString);
        }
    }
}