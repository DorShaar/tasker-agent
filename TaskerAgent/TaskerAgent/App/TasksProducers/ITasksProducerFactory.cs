using System.Collections.Generic;
using TaskData.WorkTasks;
using TaskerAgent.Domain;

namespace TaskerAgent.App.TasksProducers
{
    public interface ITasksProducerFactory
    {
        public IWorkTaskProducer CreateDailyProducer(
            Frequency frequency, MeasureType measureType, int expected, int score);

        public IWorkTaskProducer CreateWeeklyProducer(
            Frequency frequency, MeasureType measureType, Days occurrenceDays, int expected, int score);

        public IWorkTaskProducer CreateMonthlyProducer(
            Frequency frequency, MeasureType measureType, List<int> daysOfMonth, int expected, int score);
    }
}