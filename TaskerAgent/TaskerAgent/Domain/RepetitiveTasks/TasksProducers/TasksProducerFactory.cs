using System;
using TaskData.WorkTasks;
using TaskerAgent.App.TasksProducers;

namespace TaskerAgent.Domain.RepetitiveTasks.TasksProducers
{
    public class TasksProducerFactory : ITasksProducerFactory
    {
        public IWorkTaskProducer CreateProducer(Frequency frequency, MeasureType measureType, int expected, int score)
        {
            if (frequency == Frequency.Daily)
                return new DailyRepetetiveTaskProducer(frequency, measureType, expected, score);

            if (frequency == Frequency.Weekly)
                return new WeeklyRepetetiveTaskProducer(frequency, measureType, expected, score);

            throw new NotImplementedException();
        }
    }
}