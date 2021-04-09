using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;
using Triangle.Time;

namespace TaskerAgent.Infra.Services.AgentTiming
{
    public class AgentTimingService: IDisposable, IAsyncDisposable
    {
        private bool mDisposed;

        private const string LastDateUserReportedFeedbackFileName = "last_date_user_reported_feedback";
        private const int DailySummaryTime = 7;
        private const DayOfWeek WeeklySummaryTime = DayOfWeek.Sunday;

        private bool mWasResetOnMidnightAlreadyPerformed;
        private bool mWasUpdateAlreadyPerformed;
        private bool mWasDailySummarySent;
        private bool mWasWeeklySummarySent;
        private DateTime mLastDateUserReportedAFeedback;

        private readonly IOptionsMonitor<TaskerAgentConfiguration> mOptions;
        private readonly ILogger<AgentTimingService> mLogger;

        public AgentTimingService(IOptionsMonitor<TaskerAgentConfiguration> options,
            ILogger<AgentTimingService> logger)
        {
            mOptions = options ?? throw new ArgumentNullException(nameof(options));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            ReadLastDateUserReportedAsFeedback();
        }

        private void ReadLastDateUserReportedAsFeedback()
        {
            try
            {
                string lastDateUserReportedAFeedback = File.ReadAllText(GetLastDatePath());
                mLastDateUserReportedAFeedback = DateTime.Parse(lastDateUserReportedAFeedback);
            }
            catch (Exception)
            {
                mLogger.LogWarning($"Could not read {LastDateUserReportedFeedbackFileName} properly." +
                    "User feedbacks requests might be harmed");

                mLastDateUserReportedAFeedback = DateTime.Now;
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
            IOrderedEnumerable<DateTime> orderedDates =
                datesGivenFeedbackByUser.OrderBy(date => date.Ticks);

            foreach (DateTime dateTime in orderedDates)
            {
                if (mLastDateUserReportedAFeedback.AddDays(1).Date == dateTime.Date)
                    mLastDateUserReportedAFeedback = dateTime;
            }
        }

        private string GetLastDatePath()
        {
            return Path.Combine(mOptions.CurrentValue.DatabaseDirectoryPath, LastDateUserReportedFeedbackFileName);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            mLogger.LogDebug("Closing timer");

            if (mDisposed)
                return;

            if (disposing)
            {
                string lastDateUserReportedAFeedback =
                    mLastDateUserReportedAFeedback.ToString(TimeConsts.TimeFormat);

                File.WriteAllText(GetLastDatePath(), lastDateUserReportedAFeedback);

                mLogger.LogInformation($"Updated last date user reported a feedback {lastDateUserReportedAFeedback }");
            }

            mDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}