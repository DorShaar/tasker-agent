using Newtonsoft.Json;
using TaskData.TaskStatus;
using TaskData.WorkTasks;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WhyMeasureableTask : WorkTask
    {
        [JsonProperty]
        public Frequency Frequency { get; }

        internal WhyMeasureableTask(string id,
            string description,
            Frequency frequency) : base(id, description)
        {
            Frequency = frequency;
        }

        [JsonConstructor]
        internal WhyMeasureableTask(string id,
            string groupName,
            string description,
            ITaskStatusHistory taskStatusHistory,
            TaskTriangle taskTriangle,
            Frequency frequency) : base(id, groupName, description, taskStatusHistory, taskTriangle)
        {
            Frequency = frequency;
        }
    }
}