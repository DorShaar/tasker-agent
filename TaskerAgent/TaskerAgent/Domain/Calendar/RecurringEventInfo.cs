using System;

namespace TaskerAgent.Domain.Calendar
{
    public class RecurringEventInfo : EventInfo
    {
        public string RecurringEventID { get; }

        public RecurringEventInfo(string id,
            string name,
            string status,
            DateTime startTime,
            DateTime endTime,
            string recurringEventId) : base(id, name, status, startTime, endTime)
        {
            RecurringEventID = recurringEventId;
        }
    }
}