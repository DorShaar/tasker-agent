using System;
using System.Collections.Generic;
using System.Linq;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.RepetitiveTasks;
using TaskerAgent.Domain;
using TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks;
using TaskerAgent.Domain.TaskGroup;

namespace TaskerAgent.Infra.Extensions
{
    public static class TasksGroupExtensions
    {
        public static ComparisonResult Compare(this ITasksGroup currentTaskGroup, ITasksGroup taskGroupToCompareWith)
        {
            if (currentTaskGroup.ID != taskGroupToCompareWith.ID)
                return ComparisonResult.TasksAddedOrRemoved;

            if (!currentTaskGroup.Name.Equals(taskGroupToCompareWith.Name, StringComparison.OrdinalIgnoreCase))
                return ComparisonResult.TasksAddedOrRemoved;

            if (currentTaskGroup.Size != taskGroupToCompareWith.Size)
                return ComparisonResult.TasksAddedOrRemoved;

            List<IWorkTask> currentTasks = currentTaskGroup.GetAllTasks().ToList();
            List<IWorkTask> tasksToComparewith = taskGroupToCompareWith.GetAllTasks().ToList();

            if (currentTasks.Count != tasksToComparewith.Count)
                return ComparisonResult.TasksAddedOrRemoved;

            for (int i = 0; i < currentTasks.Count; ++i)
            {
                IWorkTask currentTask = currentTasks[i];
                IWorkTask taskToCompareWith = tasksToComparewith[i];
                ComparisonResult tasksComparisonResult = CompareTasks(currentTask, taskToCompareWith);
                if (tasksComparisonResult == ComparisonResult.Equal)
                    continue;

                return tasksComparisonResult;
            }

            return ComparisonResult.Equal;
        }

        private static ComparisonResult CompareTasks(IWorkTask currentTask, IWorkTask taskToCompareWith)
        {
            if (currentTask.ID != taskToCompareWith.ID)
                return ComparisonResult.TasksAddedOrRemoved;

            if (!currentTask.Description.Equals(taskToCompareWith.Description, StringComparison.OrdinalIgnoreCase))
                return ComparisonResult.TasksAddedOrRemoved;

            if (currentTask.Status != taskToCompareWith.Status)
                return ComparisonResult.TasksContentChanged;

            if (currentTask is BaseRepetitiveMeasureableTask currentGeneralTask &&
                taskToCompareWith is BaseRepetitiveMeasureableTask generalTaskToCompareWith)
            {
                return CompareGeneralTasks(currentGeneralTask, generalTaskToCompareWith);
            }

            return ComparisonResult.NoResult;
        }

        private static ComparisonResult CompareGeneralTasks(BaseRepetitiveMeasureableTask currentTask,
            BaseRepetitiveMeasureableTask taskToCompareWith)
        {
            if (currentTask.Actual != taskToCompareWith.Actual)
                return ComparisonResult.TasksContentChanged;

            if (currentTask.Expected != taskToCompareWith.Expected)
                return ComparisonResult.TasksContentChanged;

            if (currentTask.Frequency != taskToCompareWith.Frequency)
                return ComparisonResult.TasksContentChanged;

            if (currentTask.MeasureType != taskToCompareWith.MeasureType)
                return ComparisonResult.TasksContentChanged;

            if (currentTask.OccurrenceDays != taskToCompareWith.OccurrenceDays)
                return ComparisonResult.TasksContentChanged;

            if (currentTask.Score != taskToCompareWith.Score)
                return ComparisonResult.TasksContentChanged;

            if (currentTask is MonthlyRepetitiveMeasureableTask currentMonthlyTask &&
                taskToCompareWith is MonthlyRepetitiveMeasureableTask monthlyTaskToCompareWith)
            {
                return CompareMonthlyTasks(currentMonthlyTask, monthlyTaskToCompareWith);
            }

            return ComparisonResult.Equal;
        }

        private static ComparisonResult CompareMonthlyTasks(MonthlyRepetitiveMeasureableTask currentTask,
            MonthlyRepetitiveMeasureableTask taskToCompareWith)
        {
            if (currentTask.DaysOfMonth.Except(taskToCompareWith.DaysOfMonth).Any() &&
                taskToCompareWith.DaysOfMonth.Except(currentTask.DaysOfMonth).Any())
            {
                return ComparisonResult.TasksContentChanged;
            }

            return ComparisonResult.Equal;
        }

        public static IEnumerable<IRepetitiveTask> GetTasksByFrequency(this ITasksGroup currentTaskGroup, Frequency frequency)
        {
            foreach (IRepetitiveTask repetitiveTask in currentTaskGroup.GetAllTasks())
            {
                if (repetitiveTask.Frequency == frequency)
                    yield return repetitiveTask;
            }
        }
    }
}