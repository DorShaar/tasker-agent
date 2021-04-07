using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra.HostedServices
{
    public class AgentTimingService: IDisposable, IAsyncDisposable
    {
        private const string LastDateUserReportedFeedbackFileName = "last_date_user_reported_feedback";
        private const int DailySummaryTime = 7;
        private const DayOfWeek WeeklySummaryTime = DayOfWeek.Sunday;

        private bool mWasResetOnMidnightAlreadyPerformed;
        private bool mWasUpdateAlreadyPerformed;
        private bool mWasDailySummarySent;
        private bool mWasWeeklySummarySent;
        private DateTime mLastDateUserReportedAFeedback = DateTime.Now;

        private readonly ILogger<AgentTimingService> mLogger;

        public AgentTimingService(IOptionsMonitor<TaskerAgentConfiguration> options,
            ILogger<AgentTimingService> logger)
        {
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            string lastDatePath = Path.Combine(options.CurrentValue.DatabaseDirectoryPath, LastDateUserReportedFeedbackFileName);

            try
            {
                string lastDateUserReportedAFeedback = File.ReadAllText(lastDatePath);
                mLastDateUserReportedAFeedback = DateTime.Parse(lastDateUserReportedAFeedback);
            }
            catch (Exception)
            {
                mLogger.LogWarning($"Could not read {LastDateUserReportedFeedbackFileName} properly." +
                    "User feedbacks requests might be harmed");
            }
        }

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

        public void SignalDatesGivenFeedbackByUser(IEnumerable<DateTime> datesGivenFeedbackByUser)
        {
            // TODO order by date.
            foreach(DateTime dateTime in datesGivenFeedbackByUser)
            {
                if (mLastDateUserReportedAFeedback < dateTime)
                    mLastDateUserReportedAFeedback = dateTime;
            }
        }
    }
}