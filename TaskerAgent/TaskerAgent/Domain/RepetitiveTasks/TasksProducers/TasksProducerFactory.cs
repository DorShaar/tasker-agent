using System.Collections.Generic;
using TaskData.WorkTasks;
using TaskerAgent.App.TasksProducers;

namespace TaskerAgent.Domain.RepetitiveTasks.TasksProducers
{
    public class TasksProducerFactory : ITasksProducerFactory
    {
        public IWorkTaskProducer CreateDailyProducer(Frequency frequency, MeasureType measureType, int expected, int score)
        {
            return new DailyRepetetiveTaskProducer(frequency, measureType, expected, score);
        }

        public IWorkTaskProducer CreateWeeklyProducer(Frequency frequency, MeasureType measureType, Days occurrenceDays, int expected, int score)
        {
            return new WeeklyRepetetiveTaskProducer(frequency, measureType, occurrenceDays, expected, score);
        }

        public IWorkTaskProducer CreateMonthlyProducer(Frequency frequency, MeasureType measureType, List<int> daysOfMonth, int expected, int score)
        {
            return new MonthlyRepetetiveTaskProducer(frequency, measureType, daysOfMonth, expected, score);
        }
    }
}