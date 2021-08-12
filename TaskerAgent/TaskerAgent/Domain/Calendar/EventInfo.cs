using System;

namespace TaskerAgent.Domain.Calendar
{
    public class EventInfo
    {
        public string ServerId { get; }
        public string LocalId { get; }
        public string Name { get; }
        public string Status { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }

        public EventInfo(string serverId,
            string localId,
            string name,
            string status,
            DateTime startTime,
            DateTime endTime)
        {
            ServerId = serverId;
            LocalId = localId;
            Name = name;
            Status = status;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}