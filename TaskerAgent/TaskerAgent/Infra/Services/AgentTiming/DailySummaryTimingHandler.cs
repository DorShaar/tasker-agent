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
    public class DailySummaryTimingHandler : IDisposable, IAsyncDisposable
    {
        private const string MissingDatesUserReportedFeedbackFileName = "missing_dates_user_reported_feedback";

        private readonly HashSet<DateTime> mMissingeDatesUserReportedAFeedback = new HashSet<DateTime>();
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mOptions;
        private readonly ILogger<DailySummaryTimingHandler> mLogger;

        private bool mDisposed;

        protected bool mIsOperationDone;

        public bool Value
        {
            get
            {
                return mIsOperationDone;
            }
        }

        public DailySummaryTimingHandler(IOptionsMonitor<TaskerAgentConfiguration> options, ILogger<DailySummaryTimingHandler> logger)
        {
            mOptions = options ?? throw new ArgumentNullException(nameof(options));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            UpdateMissingRecievedEmailDates().Wait();
        }

        private async Task UpdateMissingRecievedEmailDates()
        {
            try
            {
                foreach (string dateLine in await File.ReadAllLinesAsync(GetMissingRecievedEmailDatesFilePath()).ConfigureAwait(false))
                {
                    if (!DateTime.TryParse(dateLine, out DateTime time))
                    {
                        mLogger.LogError($"Could not parse {dateLine} as date time from {MissingDatesUserReportedFeedbackFileName}");
                        continue;
                    }

                    mMissingeDatesUserReportedAFeedback.Add(time);
                }

                mMissingeDatesUserReportedAFeedback.Add(DateTime.Now);
            }
            catch (Exception)
            {
                mLogger.LogError($"Could not read {MissingDatesUserReportedFeedbackFileName} properly");
            }
        }

        public void SetDone(DateTime date)
        {
            mIsOperationDone = true;
            mMissingeDatesUserReportedAFeedback.Remove(date.Date);
        }

        public void SetNotDone()
        {
            mIsOperationDone = false;
        }

        public bool ShouldCheckIfDailySummaryWasSent(DateTime dateTime)
        {
            if (mIsOperationDone)
                return false;

            if (dateTime.Hour == mOptions.CurrentValue.TimeToNotify)
                return true;

            return dateTime.Date != DateTime.Now.Date && IsContainMissingDate(dateTime.Date);
        }

        public bool IsContainMissingDate(DateTime dateTime)
        {
            return mMissingeDatesUserReportedAFeedback.Contains(dateTime.Date);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            mLogger.LogDebug($"Closing {nameof(DailySummaryTimingHandler)}");

            if (mDisposed)
                return;

            if (disposing)
            {
                WriteMissingRecievedEmailDates().Wait();
            }

            mDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        private async Task WriteMissingRecievedEmailDates()
        {
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                foreach (DateTime datetime in mMissingeDatesUserReportedAFeedback)
                {
                    stringBuilder.AppendLine(datetime.ToString(TimeConsts.TimeFormat));
                }

                await File.WriteAllTextAsync(
                    GetMissingRecievedEmailDatesFilePath(), stringBuilder.ToString().Trim()).ConfigureAwait(false);

                mLogger.LogInformation($"Updated missing user's feedback reports at {MissingDatesUserReportedFeedbackFileName}");
            }
            catch (Exception)
            {
                mLogger.LogWarning($"Could not write {MissingDatesUserReportedFeedbackFileName} properly");
            }
        }

        private string GetMissingRecievedEmailDatesFilePath()
        {
            return Path.Combine(mOptions.CurrentValue.DatabaseDirectoryPath, MissingDatesUserReportedFeedbackFileName);
        }
    }
}