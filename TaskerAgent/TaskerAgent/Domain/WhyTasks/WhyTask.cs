using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskData.TaskStatus;
using TaskData.WorkTasks;
using TaskerAgent.Domain.TaskerDateTime;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WhyTask : WorkTask
    {
        [JsonProperty]
        public Frequency Frequency { get; set; }

        internal WhyTask(string id, string description, Frequency frequency, List<string> subTasks) : base(id, description)
        {
            Frequency = frequency;
            TaskTriangleBuilder taskTriangleBuilder = new TaskTriangleBuilder();

            taskTriangleBuilder.SetTime(DateTimeUtilities.GetNextDay(), TimeSpan.FromMinutes(5));

            foreach(string subTask in subTasks)
            {
                taskTriangleBuilder.AddContent(subTask);
            }

            SetMeasurement(taskTriangleBuilder.Build());
        }

        [JsonConstructor]
        internal WhyTask(string id,
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