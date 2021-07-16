using TaskData.WorkTasks;
using TaskData.WorkTasks.Producers;
using TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks;

namespace TaskerAgent.Domain.RepetitiveTasks.TasksProducers
{
    public class WhyTaskProducer : IWorkTaskProducer
    {
        public Frequency Frequency { get; }

        public WhyTaskProducer(Frequency frequency)
        {
            Frequency = frequency;
        }

        public IWorkTask ProduceTask(string id, string description)
        {
            return new WhyMeasureableTask(id, description, Frequency);
        }
    }
}