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
        private const string MissingDatesUserRecievedSummaryMailFileName = "missing_dates_user_recieved_summary_email";
        private const DayOfWeek WeeklySummaryTime = DayOfWeek.Sunday;

        private bool mWasResetOnMidnightAlreadyPerformed;

        private readonly HashSet<DateTime> mMissingeDatesUserReportedAFeedback = new HashSet<DateTime>();
        private readonly HashSet<DateTime> mMissingeDatesUserRecievedSummaryMail = new HashSet<DateTime>();
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mOptions;
        private readonly ILogger<AgentTimingService> mLogger;

        public AgentServiceHandler UpdateTasksFromInputFileHadnler = new AgentServiceHandler();
        public DailySummaryHandler DailySummarySentHandler = new DailySummaryHandler();
        public AgentServiceHandler WeeklySummarySentHandler = new AgentServiceHandler();

        public AgentTimingService(IOptionsMonitor<TaskerAgentConfiguration> options,
            ILogger<AgentTimingService> logger)
        {
            mOptions = options ?? throw new ArgumentNullException(nameof(options));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            UpdateMissingDates().Wait();
            DailySummarySentHandler.DailySummarySet += DailySummarySentHandler_DailySummarySet;
        }

        private void DailySummarySentHandler_DailySummarySet(object sender, DateTimeEventArgs e)
        {
            mMissingeDatesUserRecievedSummaryMail.Remove(DateTime.Parse(e.DateTimeString));
        }

        private async Task UpdateMissingDates()
        {
            await UpdateMissingFeedbackReportsDates().ConfigureAwait(false);
            await UpdateMissingRecievedEmailDates().ConfigureAwait(false);
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

                mMissingeDatesUserReportedAFeedback.Add(DateTime.Today.AddDays(1));
            }
            catch (Exception)
            {
                mLogger.LogWarning($"Could not read {MissingDatesUserReportedFeedbackFileName} properly." +
                  "User feedbacks requests might be harmed");

                mMissingeDatesUserReportedAFeedback.Add(DateTime.Today.AddDays(1));
            }
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

                mMissingeDatesUserRecievedSummaryMail.Add(DateTime.Today.AddDays(1));
            }
            catch (Exception)
            {
                mLogger.LogWarning($"Could not read {MissingDatesUserRecievedSummaryMailFileName} properly." +
                  "User feedbacks requests might be harmed");

                mMissingeDatesUserRecievedSummaryMail.Add(DateTime.Today.AddDays(1));
            }
        }

        public void ResetOnMidnight(DateTime dateTime)
        {
            if (dateTime.Hour == 0)
            {
                if (!mWasResetOnMidnightAlreadyPerformed)
                {
                    UpdateTasksFromInputFileHadnler.SetOff();
                    DailySummarySentHandler.SetOff();
                    WeeklySummarySentHandler.SetOff();

                    mWasResetOnMidnightAlreadyPerformed = true;
                }

                return;
            }

            mWasResetOnMidnightAlreadyPerformed = false;
        }

        public bool ShouldSendDailySummary(DateTime dateTime)
        {
            return !DailySummarySentHandler.Value && dateTime.Hour == mOptions.CurrentValue.TimeToNotify;
        }

        public bool ShouldSendWeeklySummary(DateTime dateTime)
        {
            return !WeeklySummarySentHandler.Value &&
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
                WriteMissingRecievedEmailDates().Wait();
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

                mLogger.LogInformation($"Updated missing recieved emails at {MissingDatesUserReportedFeedbackFileName}");
            }
            catch (Exception)
            {
                mLogger.LogWarning($"Could not write {MissingDatesUserRecievedSummaryMailFileName} properly." +
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

        private string GetMissingRecievedEmailDatesFilePath()
        {
            return Path.Combine(mOptions.CurrentValue.DatabaseDirectoryPath, MissingDatesUserRecievedSummaryMailFileName);
        }
    }
}