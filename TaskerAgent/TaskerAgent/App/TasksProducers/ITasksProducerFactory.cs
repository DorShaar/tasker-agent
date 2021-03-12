using TaskData.WorkTasks;
using TaskerAgent.Domain;

namespace TaskerAgent.App.TasksProducers
{
    public interface ITasksProducerFactory
    {
        public IWorkTaskProducer CreateProducer(Frequency frequency, MeasureType measureType, int expected, int score);
    }
}