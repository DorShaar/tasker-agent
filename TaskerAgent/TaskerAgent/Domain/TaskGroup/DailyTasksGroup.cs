using Newtonsoft.Json;
using System.Collections.Generic;
using TaskData.Notes;
using TaskData.TasksGroups;
using TaskData.WorkTasks;

namespace TaskerAgent.Domain.TaskGroup
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DailyTasksGroup : TasksGroup
    {
        [JsonProperty]
        public INote Note { get; }

        [JsonProperty]
        public bool IsAlreadyReported { get; private set; }

        public DailyTasksGroup(string id, string groupName) : base(id, groupName)
        {
        }

        [JsonConstructor]
        public DailyTasksGroup(string id,
            string name,
            Dictionary<string, IWorkTask> tasksChildren,
            INote note,
            bool isAlreadyReported) :
            base(id, name, tasksChildren)
        {
            Note = note;
            IsAlreadyReported = isAlreadyReported;
        }

        public void SetReported()
        {
            IsAlreadyReported = true;
        }
    }
}