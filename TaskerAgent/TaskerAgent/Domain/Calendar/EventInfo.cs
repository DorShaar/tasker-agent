using System;

namespace TaskerAgent.Domain.Calendar
{
    public class EventInfo
    {
        public string Id { get; }
        public string Name { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }

        public EventInfo(string id,
            string name,
            DateTime startTime,
            DateTime endTime)
        {
            Id = id;
            Name = name;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}