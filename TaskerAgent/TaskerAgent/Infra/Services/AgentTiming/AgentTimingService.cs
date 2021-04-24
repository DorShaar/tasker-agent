using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;
using Triangle.Time;

namespace TaskerAgent.Infra.Services.AgentTiming
{
    public class AgentTimingService : IDisposable, IAsyncDisposable
    {
        private bool mDisposed;

        private const string MissingDatesUserReportedFeedbackFileName = "missing_dates_user_reported_feedback";
        private const DayOfWeek WeeklySummaryTime = DayOfWeek.Sunday;

        private bool mWasResetOnMidnightAlreadyPerformed;

        private readonly HashSet<DateTime> mMissingeDatesUserReportedAFeedback = new HashSet<DateTime>();
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mOptions;
        private readonly ILogger<AgentTimingService> mLogger;

        public AgentServiceHandler UpdateTasksFromInputFileHandler = new AgentServiceHandler();
        public AgentServiceHandler TodaysFutureReportHandler = new AgentServiceHandler();
        public AgentServiceHandler WeeklySummarySentHandler = new AgentServiceHandler();
        public DailySummaryTimingHandler DailySummarySentTimingHandler;

        public AgentTimingService(IOptionsMonitor<TaskerAgentConfiguration> options,
            ILoggerFactory loggerFactory)
        {
            mOptions = options ?? throw new ArgumentNullException(nameof(options));
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            mLogger = loggerFactory.CreateLogger<AgentTimingService>();
            DailySummarySentTimingHandler = new DailySummaryTimingHandler(options, loggerFactory.CreateLogger<DailySummaryTimingHandler>());
            UpdateMissingFeedbackReportsDates().Wait();
        }

        private async Task UpdateMissingFeedbackReportsDates()
        {
            try
            {
                foreach (string dateLine in await File.ReadAllLinesAsync(GetMissingFeedbackReportsDatesFilePath()).ConfigureAwait(false))
                {
                    if (!DateTime.TryParse(dateLine, out DateTime time))
                    {
                        mLogger.LogError($"Could not parse {dateLine} as date time from {MissingDatesUserReportedFeedbackFileName}");
                        continue;
                    }

                    mMissingeDatesUserReportedAFeedback.Add(time);
                }
            }
            catch (Exception)
            {
                mLogger.LogWarning($"Could not read {MissingDatesUserReportedFeedbackFileName} properly." +
                  "User feedbacks requests might be harmed");
            }
        }

        public void ResetOnMidnight(DateTime dateTime)
        {
            if (dateTime.Hour == 0)
            {
                if (!mWasResetOnMidnightAlreadyPerformed)
                {
                    UpdateTasksFromInputFileHandler.SetNotDone();
                    DailySummarySentTimingHandler.SetOff();
                    WeeklySummarySentHandler.SetNotDone();

                    mWasResetOnMidnightAlreadyPerformed = true;
                }

                return;
            }

            mWasResetOnMidnightAlreadyPerformed = false;
        }

        public bool ShouldSendDailySummary(DateTime dateTime)
        {
            return DailySummarySentTimingHandler.ShouldSendDailySummary(dateTime);
        }

        public bool ShouldSendWeeklySummary(DateTime dateTime)
        {
            return !WeeklySummarySentHandler.ShouldDo &&
                dateTime.Hour == mOptions.CurrentValue.TimeToNotify &&
                dateTime.DayOfWeek == WeeklySummaryTime;
        }

        public void SignalDatesGivenFeedbackByUser(IEnumerable<DateTime> datesGivenFeedbackByUser)
        {
            foreach (DateTime dateTime in datesGivenFeedbackByUser)
            {
                mMissingeDatesUserReportedAFeedback.Remove(dateTime.Date);
            }
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
                WriteMissingFeedbackReportsDates().Wait();
                DailySummarySentTimingHandler.Dispose();
            }

            mDisposed = true;
        }

        private async Task WriteMissingFeedbackReportsDates()
        {
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                foreach (DateTime datetime in mMissingeDatesUserReportedAFeedback)
                {
                    stringBuilder.AppendLine(datetime.ToString(TimeConsts.TimeFormat));
                }

                await File.WriteAllTextAsync(
                    GetMissingFeedbackReportsDatesFilePath(), stringBuilder.ToString().Trim()).ConfigureAwait(false);

                mLogger.LogInformation($"Updated missing user's feedback reports at {MissingDatesUserReportedFeedbackFileName}");
            }
            catch (Exception)
            {
                mLogger.LogWarning($"Could not write {MissingDatesUserReportedFeedbackFileName} properly." +
                  "User feedbacks requests might be harmed");
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        private string GetMissingFeedbackReportsDatesFilePath()
        {
            return Path.Combine(mOptions.CurrentValue.DatabaseDirectoryPath, MissingDatesUserReportedFeedbackFileName);
        }
    }
}