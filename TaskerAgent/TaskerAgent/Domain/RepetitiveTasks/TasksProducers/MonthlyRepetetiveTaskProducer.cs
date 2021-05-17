using System.Collections.Generic;
using TaskData.WorkTasks;
using TaskData.WorkTasks.Producers;

namespace TaskerAgent.Domain.RepetitiveTasks.TasksProducers
{
    public class MonthlyRepetetiveTaskProducer : IWorkTaskProducer
    {
        public Frequency Frequency { get; }
        public MeasureType MeasureType { get; }
        public List<int> DaysOfMonth { get; }
        public int Expected { get; }
        public int Score { get; }

        public MonthlyRepetetiveTaskProducer(Frequency frequency, MeasureType measureType, List<int> daysOfMonth, int expected, int score)
        {
            Frequency = frequency;
            MeasureType = measureType;
            DaysOfMonth = daysOfMonth;
            Expected = expected;
            Score = score;
        }

        public IWorkTask ProduceTask(string id, string description)
        {
            return new MonthlyRepetitiveMeasureableTask(id, description, Frequency, MeasureType, DaysOfMonth, Expected, Score);
        }
    }
}