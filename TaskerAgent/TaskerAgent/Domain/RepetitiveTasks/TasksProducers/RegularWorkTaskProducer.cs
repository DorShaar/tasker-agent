using System;
using TaskData.WorkTasks;
using TaskData.WorkTasks.Producers;
using Triangle;

namespace TaskerAgent.Domain.RepetitiveTasks.TasksProducers
{
    public class RegularWorkTaskProducer : IWorkTaskProducer
    {
        private readonly WorkTaskProducer mWorkTaskProducer;
        public DateTime DateTime { get; }

        public RegularWorkTaskProducer(WorkTaskProducer workTaskProducer, DateTime dateTime)
        {
            mWorkTaskProducer = workTaskProducer;
            DateTime = dateTime;
        }

        public IWorkTask ProduceTask(string id, string description)
        {
            IWorkTask workTask = mWorkTaskProducer.ProduceTask(id, description);
            TaskTriangleBuilder taskTriangleBuilder = new TaskTriangleBuilder();

            taskTriangleBuilder.AddContent(description).SetTime(DateTime, TimeSpan.Zero);
            workTask.SetMeasurement(taskTriangleBuilder.Build());

            return workTask;
        }
    }
}