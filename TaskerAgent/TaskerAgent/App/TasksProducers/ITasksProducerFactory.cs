using System.Collections.Generic;
using TaskData.WorkTasks.Producers;
using TaskerAgent.Domain;

namespace TaskerAgent.App.TasksProducers
{
    public interface ITasksProducerFactory
    {
        public IWorkTaskProducer CreateDailyProducer(MeasureType measureType, int expected, int score);

        public IWorkTaskProducer CreateWeeklyProducer(MeasureType measureType, Days occurrenceDays, int expected, int score);
        public IWorkTaskProducer CreateDuWeeklyProducer(MeasureType measureType, Days occurrenceDays, int expected, int score);

        public IWorkTaskProducer CreateMonthlyProducer(MeasureType measureType, List<int> daysOfMonth, int expected, int score);
        public IWorkTaskProducer CreateWhyTasksProducer(Frequency frequency);
    }
}