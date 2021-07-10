using TaskData.WorkTasks;
using TaskData.WorkTasks.Producers;
using TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks;

namespace TaskerAgent.Domain.RepetitiveTasks.TasksProducers
{
    public class WeeklyRepetetiveTaskProducer : IWorkTaskProducer
    {
        public Frequency Frequency { get; }
        public MeasureType MeasureType { get; }
        public Days OccurrenceDays { get; }
        public int Expected { get; }
        public int Score { get; }

        public WeeklyRepetetiveTaskProducer(Frequency frequency, MeasureType measureType, Days occurrenceDays, int expected, int score)
        {
            Frequency = frequency;
            MeasureType = measureType;
            OccurrenceDays = occurrenceDays;
            Expected = expected;
            Score = score;
        }

        public IWorkTask ProduceTask(string id, string description)
        {
            return new WeeklyRepetitiveMeasureableTask(id, description, Frequency, MeasureType, OccurrenceDays, Expected, Score);
        }
    }
}