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
        private const string MissingDatesUserRecievedSummaryMailFileName = "missing_dates_user_recieved_summary_email";

        private readonly HashSet<DateTime> mMissingeDatesUserRecievedSummaryMail = new HashSet<DateTime>();
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
                        mLogger.LogError($"Could not parse {dateLine} as date time from {MissingDatesUserRecievedSummaryMailFileName}");
                        continue;
                    }

                    mMissingeDatesUserRecievedSummaryMail.Add(time);
                }
            }
            catch (Exception)
            {
                mLogger.LogWarning($"Could not read {MissingDatesUserRecievedSummaryMailFileName} properly");
            }
        }

        public void SetDone(DateTime date)
        {
            mIsOperationDone = true;
            mMissingeDatesUserRecievedSummaryMail.Remove(date);
        }

        public void SetNotDone()
        {
            mIsOperationDone = false;
        }

        public bool ShouldSendDailySummary(DateTime dateTime)
        {
            if (mIsOperationDone)
                return false;

            if (dateTime.Hour == mOptions.CurrentValue.TimeToNotify)
                return true;

            return mMissingeDatesUserRecievedSummaryMail.Count > 0;
        }

        public IEnumerable<DateTime> GetMissingeDatesUserRecievedSummaryMail()
        {
            DateTime[] dateTimes = new DateTime[mMissingeDatesUserRecievedSummaryMail.Count];
            mMissingeDatesUserRecievedSummaryMail.CopyTo(dateTimes);
            return dateTimes;
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
                foreach (DateTime datetime in mMissingeDatesUserRecievedSummaryMail)
                {
                    stringBuilder.AppendLine(datetime.ToString(TimeConsts.TimeFormat));
                }

                await File.WriteAllTextAsync(
                    GetMissingRecievedEmailDatesFilePath(), stringBuilder.ToString().Trim()).ConfigureAwait(false);

                mLogger.LogInformation($"Updated missing recieved emails from {MissingDatesUserRecievedSummaryMailFileName}");
            }
            catch (Exception)
            {
                mLogger.LogWarning($"Could not write {MissingDatesUserRecievedSummaryMailFileName} properly");
            }
        }

        private string GetMissingRecievedEmailDatesFilePath()
        {
            return Path.Combine(mOptions.CurrentValue.DatabaseDirectoryPath, MissingDatesUserRecievedSummaryMailFileName);
        }
    }
}