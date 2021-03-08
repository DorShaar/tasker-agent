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
using TaskerAgent.Domain.RepetitiveTasks.TasksClusters;
using TaskerAgent.Infra.Options.Configurations;
using Triangle.Time;

namespace TaskerAgent.Infra.RepetitiveTasksUpdaters
{
    public class RepetitiveTasksUpdater
    {
        private readonly IDbRepository<ITasksGroup> mTasksGroupRepository;
        private readonly ITasksGroupFactory mTaskGroupFactory;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<RepetitiveTasksUpdater> mLogger;

        public RepetitiveTasksUpdater(IDbRepository<ITasksGroup> tasksGroupRepository,
            ITasksGroupFactory taskGroupFactory,
            IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
            ILogger<RepetitiveTasksUpdater> logger)
        {
            mTasksGroupRepository = tasksGroupRepository ?? throw new ArgumentNullException(nameof(tasksGroupRepository));
            mTaskGroupFactory = taskGroupFactory ?? throw new ArgumentNullException(nameof(taskGroupFactory));
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Update(ITasksGroup tasksGroupToUpdateAccordingly)
        {
            TasksCluster tasksCluster = TasksCluster.SplitTaskGroupByFrequency(tasksGroupToUpdateAccordingly);

            // TODO UpdateTasks - return groups.
            await UpdateTasks(tasksCluster.DailyTasks).ConfigureAwait(false);
            await UpdateTasks(tasksCluster.WeeklyTasks).ConfigureAwait(false);
            await UpdateTasks(tasksCluster.MonthlyTasks).ConfigureAwait(false);

            await mTasksGroupRepository.UpdateAsync(returnedGroups).ConfigureAwait(false);
        }

        private async Task UpdateTasks(IEnumerable<IWorkTask> dailyTasksToUpdateAccordingly)
        {
            foreach (DateTime date in GetNextDaysDates(mTaskerAgentOptions.CurrentValue.DaysToKeepForward))
            {
                string groupName = date.ToString(TimeConsts.TimeFormat);

                ITasksGroup taskGroup =
                    await mTasksGroupRepository.FindAsync(groupName).ConfigureAwait(false);

                if (taskGroup == null)
                {
                    OperationResult<ITasksGroup> taskGroupResult = mTaskGroupFactory.CreateGroup(groupName);
                    if (!taskGroupResult.Success)
                    {
                        mLogger.LogError($"Could not create group {groupName}");
                        continue;
                    }

                    taskGroup = taskGroupResult.Value;
                }

                UpdateGroup(taskGroup, dailyTasksToUpdateAccordingly);
            }
        }

        private IEnumerable<DateTime> GetNextDaysDates(int nextDays)
        {
            for (int i = 0; i < nextDays; ++i)
            {
                yield return DateTime.Now.AddDays(1 + i);
            }
        }

        private void UpdateGroup(ITasksGroup currentTaskGroup, IEnumerable<IWorkTask> tasksToUpdateAccordingly)
        {
            foreach (IWorkTask taskToUpdateAccordingly in tasksToUpdateAccordingly)
            {
                if (!(taskToUpdateAccordingly is IRepetitiveMeasureableTask repititiveTaskToUpdateAccordingly))
                    continue;

                bool isNewTask = true;

                foreach (IWorkTask currentTask in currentTaskGroup.GetAllTasks())
                {
                    if (!(currentTask is IRepetitiveMeasureableTask currentRepititiveTask))
                        continue;

                    if (taskToUpdateAccordingly.Description.Equals(currentTask.Description, StringComparison.OrdinalIgnoreCase))
                    {
                        UpdateCurrentTask(currentRepititiveTask, repititiveTaskToUpdateAccordingly);
                        currentTaskGroup.UpdateTask(currentRepititiveTask); // TODO should delete?
                        isNewTask = false;
                    }
                }

                if (isNewTask)
                    CreateNewTask(currentTaskGroup, repititiveTaskToUpdateAccordingly);
            }
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
            OperationResult<IWorkTask> newTaskResult =
                        mTaskGroupFactory.CreateTask(currentTaskGroup, taskToUpdateAccordingly.Description);

            if (!newTaskResult.Success ||
                !(newTaskResult.Value is IRepetitiveMeasureableTask repetitiveMeasureableTask))
            {
                return;
            }

            repetitiveMeasureableTask.InitializeRepetitiveMeasureableTask(taskToUpdateAccordingly.Frequency,
                taskToUpdateAccordingly.MeasureType,
                taskToUpdateAccordingly.Expected,
                score: 1);

            currentTaskGroup.UpdateTask(repetitiveMeasureableTask); // TODO should delete?
        }
    }
}