using System;
using TaskData.WorkTasks;
using TaskerAgent.Domain;

namespace TaskerAgent.App.RepetitiveTasks
{
    public interface IRepetitiveTask : IWorkTask
    {
        Frequency Frequency { get; }
        Days OccurrenceDays { get; }
        Days FromDayOfWeek(DayOfWeek dayOfWeek);
    }
}