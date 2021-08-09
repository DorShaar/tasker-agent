using System;

namespace TaskerAgent.Domain.Calendar
{
    public class EventInfo
    {
        public string Id { get; }
        public string Name { get; }
        public string Status { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }

        public EventInfo(string id,
            string name,
            string status,
            DateTime startTime,
            DateTime endTime)
        {
            Id = id;
            Name = name;
            Status = status;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}