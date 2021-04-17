using System.Collections.Generic;
using TaskData.TasksGroups;
using TaskData.WorkTasks;

namespace TaskerAgent.Infra.Extensions
{
    public static class TasksGroupExtensions
    {
        public static bool Compare(this ITasksGroup currentTaskGroup, ITasksGroup taskGroupToCompareWith)
        {
            IEnumerable<IWorkTask> currentTasks = currentTaskGroup.GetAllTasks()
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
}