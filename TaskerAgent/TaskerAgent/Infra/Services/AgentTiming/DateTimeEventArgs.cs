using System;

namespace TaskerAgent.Infra.Services.AgentTiming
{
    public class DateTimeEventArgs : EventArgs
    {
        public string DateTimeString { get; }

        public DateTimeEventArgs(string date)
        {
            DateTimeString = date;
        }
    }
}