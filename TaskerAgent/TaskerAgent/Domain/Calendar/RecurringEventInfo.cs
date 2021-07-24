using System;

namespace TaskerAgent.Domain.Calendar
{
    public class RecurringEventInfo : EventInfo
    {
        public string RecurringEventID { get; }

        public RecurringEventInfo(string id,
            string name,
            DateTime startTime,
            DateTime endTime,
            string recurringEventId) : base(id, name, startTime, endTime)
        {
            RecurringEventID = recurringEventId;
        }
    }
}