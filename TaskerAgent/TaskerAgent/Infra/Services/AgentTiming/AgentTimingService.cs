using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra.Services.AgentTiming
{
    public class AgentTimingService : IDisposable, IAsyncDisposable
    {
        private const DayOfWeek WeeklySummaryTime = DayOfWeek.Sunday;

        private bool mDisposed;
        private bool mWasResetOnMidnightAlreadyPerformed;

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
        }

        public void ResetOnMidnight(DateTime dateTime)
        {
            if (dateTime.Hour == 0)
            {
                if (!mWasResetOnMidnightAlreadyPerformed)
                {
                    UpdateTasksFromInputFileHandler.SetNotDone();
                    DailySummarySentTimingHandler.SetNotDone();
                    WeeklySummarySentHandler.SetNotDone();

                    mWasResetOnMidnightAlreadyPerformed = true;
                }

                return;
            }

            mWasResetOnMidnightAlreadyPerformed = false;
        }

        public bool ShouldSendWeeklySummary(DateTime dateTime)
        {
            return !WeeklySummarySentHandler.ShouldDo &&
                dateTime.Hour == mOptions.CurrentValue.TimeToNotify &&
                dateTime.DayOfWeek == WeeklySummaryTime;
        }

        public void SignalDateGivenFeedbackByUser(DateTime dateGivenFeedbackByUser)
        {
            DailySummarySentTimingHandler.SetDone(dateGivenFeedbackByUser.Date);
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
                DailySummarySentTimingHandler.Dispose();
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