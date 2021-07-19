using System;

namespace TaskerAgent.Domain.Calendar
{
    public class EventInfo
    {
        public string EventName { get; set; }
        public DateTime EventStartTime { get; set; }
        public DateTime EventEndTime { get; set; }
    }
}