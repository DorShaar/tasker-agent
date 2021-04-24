using System;

namespace TaskerAgent.Infra.Services.AgentTiming
{
    public class DailySummaryHandler : AgentServiceHandler
    {
        public event EventHandler<DateTimeEventArgs> DailySummarySet;

        public void SetOn(string date)
        {
            mStatus = true;
            DailySummarySet.Invoke(this, new DateTimeEventArgs(date));
        }
    }
}