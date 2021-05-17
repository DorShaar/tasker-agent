﻿using TaskData.TasksGroups;
using TaskData.WorkTasks.Producers;

namespace TaskerAgent.Domain.TaskGroup
{
    internal class DailyTasksGroupProducer : ITasksGroupProducer
    {
        public ITasksGroup CreateGroup(string id, string groupName)
        {
            return new DailyTasksGroup(id, groupName);
        }
    }
}