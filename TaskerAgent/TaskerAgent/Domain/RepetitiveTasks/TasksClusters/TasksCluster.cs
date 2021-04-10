using System;
using System.Collections.Generic;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.RepetitiveTasks;

namespace TaskerAgent.Domain.RepetitiveTasks.TasksClusters
{
    public class TasksCluster
    {
        public IEnumerable<DailyRepetitiveMeasureableTask> DailyTasks { get; } = new List<DailyRepetitiveMeasureableTask>();
        public IEnumerable<WeeklyRepetitiveMeasureableTask> WeeklyTasks { get; } = new List<WeeklyRepetitiveMeasureableTask>();
        public IEnumerable<MonthlyRepetitiveMeasureableTask> MonthlyTasks { get; } = new List<MonthlyRepetitiveMeasureableTask>();

        public TasksCluster(IEnumerable<DailyRepetitiveMeasureableTask> dailyTasks,
            IEnumerable<WeeklyRepetitiveMeasureableTask> weeklyTasks,
            IEnumerable<MonthlyRepetitiveMeasureableTask> monthlyTasks)
        {
            DailyTasks = dailyTasks;
            WeeklyTasks = weeklyTasks;
            MonthlyTasks = monthlyTasks;
        }

        public static TasksCluster SplitTaskGroupByFrequency(ITasksGroup tasksGroup)
        {
            List<DailyRepetitiveMeasureableTask> dailyTasks = new List<DailyRepetitiveMeasureableTask>();
            List<WeeklyRepetitiveMeasureableTask> weeklyTasks = new List<WeeklyRepetitiveMeasureableTask>();
            List<MonthlyRepetitiveMeasureableTask> monthlyTasks = new List<MonthlyRepetitiveMeasureableTask>();

            foreach (IRepetitiveTask repetitiveTask in tasksGroup.GetAllTasks())
            {
                if (repetitiveTask is DailyRepetitiveMeasureableTask dailyTask)
                {
                    dailyTasks.Add(dailyTask);
                    continue;
                }

                if (repetitiveTask is WeeklyRepetitiveMeasureableTask weeklyTask)
                {
                    weeklyTasks.Add(weeklyTask);
                    continue;
                }

                if (repetitiveTask is MonthlyRepetitiveMeasureableTask monthlyTask)
                {
                    monthlyTasks.Add(monthlyTask);
                    continue;
                }

                throw new InvalidOperationException($"Task {repetitiveTask.Description} is not one of the expected repetitive tasks");
            }

            return new TasksCluster(dailyTasks, weeklyTasks, monthlyTasks);
        }
    }
}