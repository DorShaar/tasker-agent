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

        public RepetitiveTasksUpdater(IDbRepository<ITasksGroup> tasksGroupRepository,
            ITasksGroupFactory taskGroupFactory,
            IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions)
        {
            mTasksGroupRepository = tasksGroupRepository ?? throw new ArgumentNullException(nameof(tasksGroupRepository));
            mTaskGroupFactory = taskGroupFactory ?? throw new ArgumentNullException(nameof(taskGroupFactory));
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
        }

        // TODO not for this week but for the next X days.
        public bool WasUpdatePerformed(ITasksGroup tasksGroupFromConfig, IEnumerable<string> thisWeekTasks)
        {
            IEnumerable<string> tasksFromConfig = tasksGroupFromConfig.GetAllTasks()
                .Select(workTask => workTask.Description).Distinct();

            return tasksFromConfig.Except(thisWeekTasks).Any();
        }

        public async Task Update(ITasksGroup tasksGroup)
        {
            TasksCluster tasksCluster = TasksCluster.SplitTaskGroupByFrequency(tasksGroup);

            await UpdateDailyTasks(tasksCluster.DailyTasks).ConfigureAwait(false);
            //UpdateWeeklyTasks(tasksCluster.WeeklyTasks);
            //UpdateMonthlyTasks(tasksCluster.MonthlyTasks);

            await mTasksGroupRepository.UpdateAsync(tasksGroup).ConfigureAwait(false);
        }

        private async Task UpdateDailyTasks(IEnumerable<IWorkTask> dailyTasksToUpdate)
        {
            foreach (DateTime date in GetNextDaysDates(mTaskerAgentOptions.CurrentValue.DaysToKeepForward))
            {
                ITasksGroup dailyTaskGroup =
                    await mTasksGroupRepository.FindAsync(date.ToString(TimeConsts.TimeFormat)).ConfigureAwait(false);

                UpdateGroup(dailyTasksToUpdate, dailyTaskGroup);
            }
        }

        private IEnumerable<DateTime> GetNextDaysDates(int nextDays)
        {
            for (int i = 0; i < nextDays; ++i)
            {
                yield return DateTime.Now.AddDays(1 + i);
            }
        }

        private void UpdateGroup(IEnumerable<IWorkTask> dailyTasksToUpdateAccordingly, ITasksGroup currentDailyTaskGroup)
        {
            foreach (IWorkTask dailyTaskToUpdateAccordingly in dailyTasksToUpdateAccordingly)
            {
                if (!(dailyTaskToUpdateAccordingly is IRepetitiveMeasureableTask repititiveTaskToUpdateAccordingly))
                    continue;

                bool isNewTask = true;

                foreach (IWorkTask currentDailyTask in currentDailyTaskGroup.GetAllTasks())
                {
                    if (!(currentDailyTask is IRepetitiveMeasureableTask currentRepititiveTask))
                        continue;

                    if (dailyTaskToUpdateAccordingly.Description.Equals(currentDailyTask.Description, StringComparison.OrdinalIgnoreCase))
                    {
                        UpdateCurrentTask(currentDailyTaskGroup, currentRepititiveTask, repititiveTaskToUpdateAccordingly);
                        isNewTask = false;
                    }
                }

                if (isNewTask)
                    CreateNewTask(currentDailyTaskGroup, repititiveTaskToUpdateAccordingly);
            }
        }

        private void UpdateCurrentTask(ITasksGroup currentDailyTaskGroup, IRepetitiveMeasureableTask currentDailyTask,
            IRepetitiveMeasureableTask dailyTaskToUpdateAccordingly)
        {
            // TODO check if actual, expected or score was changed.
            //if (currentDailyTask.)
        }

        private void CreateNewTask(ITasksGroup currentDailyTaskGroup, IRepetitiveMeasureableTask dailyTaskToUpdateAccordingly)
        {
            OperationResult<IWorkTask> newTaskResult =
                        mTaskGroupFactory.CreateTask(currentDailyTaskGroup, dailyTaskToUpdateAccordingly.Description);

            if (!newTaskResult.Success ||
                !(newTaskResult.Value is IRepetitiveMeasureableTask repetitiveMeasureableTask))
            {
                return;
            }

            repetitiveMeasureableTask.InitializeRepetitiveMeasureableTask(dailyTaskToUpdateAccordingly.Frequency,
                dailyTaskToUpdateAccordingly.MeasureType,
                dailyTaskToUpdateAccordingly.Expected,
                score: 1);
        }

        // TODO
        //private async Task UpdateWeeklyTasks(IEnumerable<IWorkTask> weeklyTasks)
        //{

        //}

        // TODO
        //private async Task UpdateMonthlyTasks(IEnumerable<IWorkTask> monthlyTasks)
        //{

        //}
    }
}