using Newtonsoft.Json;
using System;
using System.Linq;
using TaskData.TaskStatus;
using TaskData.WorkTasks;
using TaskerAgent.Domain.TaskerDateTime;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WhyMeasureableTask : WorkTask
    {
        [JsonProperty]
        public Frequency Frequency { get; set; }

        internal WhyMeasureableTask(string id, string description, Frequency frequency) : base(id, description)
        {
            Frequency = frequency;
            TaskTriangleBuilder taskTriangleBuilder = new TaskTriangleBuilder();

            taskTriangleBuilder.SetTime(DateTimeUtilities.GetNextDay(DayOfWeek.Sunday), TimeSpan.FromMinutes(5));

            SetMeasurement(taskTriangleBuilder.Build());
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

        public void UpdateStatus()
        {
            if (TaskMeasurement.Content.GetContents().Values.All(isContentDone => isContentDone))
            {
                CloseTask("All tasks are done");
                return;
            }

            ReOpenTask("Not all task are done");
        }
    }
}