using System;

namespace TaskerAgent.Infra.HostedServices
{
    public class AgentTimingService
    {
        private const int DailySummaryTime = 7;
        private const DayOfWeek WeeklySummaryTime = DayOfWeek.Sunday;

        private bool mWasResetOnMidnightAlreadyPerformed;
        private bool mWasUpdateAlreadyPerformed;
        private bool mWasDailySummarySent;
        private bool mWasWeeklySummarySent;

        public void ResetOnMidnight(DateTime dateTime)
        {
            if (dateTime.Hour == 0)
            {
                if (!mWasResetOnMidnightAlreadyPerformed)
                {
                    mWasUpdateAlreadyPerformed = false;
                    mWasDailySummarySent = false;
                    mWasWeeklySummarySent = false;

                    mWasResetOnMidnightAlreadyPerformed = true;
                }

                return;
            }

            mWasResetOnMidnightAlreadyPerformed = false;
        }

        public bool ShouldUpdate()
        {
            return mWasUpdateAlreadyPerformed;
        }

        public void SignalUpdatePerformed()
        {
            mWasUpdateAlreadyPerformed = true;
        }

        public bool ShouldSendDailySummary(DateTime dateTime)
        {
            return !mWasDailySummarySent && dateTime.Hour == DailySummaryTime;
        }

        public void SignalDailySummaryPerformed()
        {
            mWasDailySummarySent = true;
        }

        public bool ShouldSendWeeklySummary(DateTime dateTime)
        {
            return !mWasWeeklySummarySent && dateTime.Hour == DailySummaryTime && dateTime.DayOfWeek == WeeklySummaryTime;
        }

        public void SignalWeeklySummaryPerformed()
        {
            mWasWeeklySummarySent = true;
        }
    }
}