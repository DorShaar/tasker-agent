using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra.Services.AgentTiming
{
    public class AgentTimingService
    {
        private const DayOfWeek WeeklySummaryTime = DayOfWeek.Sunday;

        private bool mWasResetOnMidnightAlreadyPerformed;

        private readonly IOptionsMonitor<TaskerAgentConfiguration> mOptions;
        private readonly ILogger<AgentTimingService> mLogger;

        public AgentServiceHandler UpdateTasksFromInputFileHandler = new AgentServiceHandler();
        public AgentServiceHandler TodaysFutureReportHandler = new AgentServiceHandler();
        public AgentServiceHandler WeeklySummarySentHandler = new AgentServiceHandler();
        public AgentServiceHandler DailySummarySentTimingHandler = new AgentServiceHandler();

        public AgentTimingService(IOptionsMonitor<TaskerAgentConfiguration> options,
            ILogger<AgentTimingService> logger)
        {
            mOptions = options ?? throw new ArgumentNullException(nameof(options));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    }
}