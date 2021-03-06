using System.Collections.Generic;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.RepetitiveTasks;

namespace TaskerAgent.Domain.RepetitiveTasks.TasksClusters
{
    public class TasksCluster
    {
        public IEnumerable<IWorkTask> DailyTasks { get; } = new List<IWorkTask>();
        public IEnumerable<IWorkTask> WeeklyTasks { get; } = new List<IWorkTask>();
        public IEnumerable<IWorkTask> MonthlyTasks { get; } = new List<IWorkTask>();

        public TasksCluster(IEnumerable<IWorkTask> dailyTasks, IEnumerable<IWorkTask> weeklyTasks, IEnumerable<IWorkTask> monthlyTasks)
        {
            DailyTasks = dailyTasks;
            WeeklyTasks = weeklyTasks;
            MonthlyTasks = monthlyTasks;
        }

        public static TasksCluster SplitTaskGroupByFrequency(ITasksGroup tasksGroup)
        {
            List<IWorkTask> dailyTasks = new List<IWorkTask>();
            List<IWorkTask> weeklyTasks = new List<IWorkTask>();
            List<IWorkTask> monthlyTasks = new List<IWorkTask>();

            foreach (IRepetitiveTask repetitiveTask in tasksGroup.GetAllTasks())
            {
                if (repetitiveTask.Frequency == Frequency.Daily)
                    dailyTasks.Add(repetitiveTask);

                if (repetitiveTask.Frequency == Frequency.Weekly)
                    weeklyTasks.Add(repetitiveTask);

                if (repetitiveTask.Frequency == Frequency.Monthly)
                    monthlyTasks.Add(repetitiveTask);
            }

            return new TasksCluster(dailyTasks, weeklyTasks, monthlyTasks);
        }
    }
}