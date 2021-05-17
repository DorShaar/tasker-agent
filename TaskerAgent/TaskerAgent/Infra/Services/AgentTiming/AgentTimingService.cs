using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra.Services.AgentTiming
{
    public class AgentTimingService
    {
        private const string TimingStatusFileName = "timing_status";
        private const DayOfWeek WeeklySummaryTime = DayOfWeek.Sunday;

        private readonly IOptionsMonitor<TaskerAgentConfiguration> mOptions;
        private readonly ILogger<AgentTimingService> mLogger;

        private bool mWasResetOnMidnightAlreadyPerformed;

        public AgentServiceHandler UpdateTasksFromInputFileHandler = new AgentServiceHandler();
        public AgentServiceHandler TodaysFutureReportHandler = new AgentServiceHandler();
        public AgentServiceHandler WeeklySummarySentHandler = new AgentServiceHandler();
        public AgentServiceHandler DailySummarySentTimingHandler = new AgentServiceHandler();

        public AgentTimingService(IOptionsMonitor<TaskerAgentConfiguration> options,
            ILogger<AgentTimingService> logger)
        {
            mOptions = options ?? throw new ArgumentNullException(nameof(options));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            ReadAgentTimingStatus().Wait();

            UpdateTasksFromInputFileHandler.UpdatePerformed += Handler_UpdatePerformed;
            TodaysFutureReportHandler.UpdatePerformed += Handler_UpdatePerformed;
            WeeklySummarySentHandler.UpdatePerformed += Handler_UpdatePerformed;
            DailySummarySentTimingHandler.UpdatePerformed += Handler_UpdatePerformed;
        }

        private void Handler_UpdatePerformed(object sender, EventArgs e)
        {
            UpdateAgentTimingStatus().Wait();
        }

        public async Task ResetOnMidnight(DateTime dateTime)
        {
            try
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
            finally
            {
                await UpdateAgentTimingStatus().ConfigureAwait(false);
            }
        }

        public bool ShouldSendWeeklySummary(DateTime dateTime)
        {
            return !WeeklySummarySentHandler.ShouldDo &&
                dateTime.Hour == mOptions.CurrentValue.TimeToNotify &&
                dateTime.DayOfWeek == WeeklySummaryTime;
        }

        private async Task UpdateAgentTimingStatus()
        {
            StringBuilder statusBuilder = new StringBuilder();

            statusBuilder.AppendJoin(',',
                mWasResetOnMidnightAlreadyPerformed.ToString(),
                UpdateTasksFromInputFileHandler.ShouldDo.ToString(),
                TodaysFutureReportHandler.ShouldDo.ToString(),
                WeeklySummarySentHandler.ShouldDo.ToString(),
                DailySummarySentTimingHandler.ShouldDo.ToString());

            await File.WriteAllTextAsync(
                GetAgentTimingStatusFilePath(), statusBuilder.ToString()).ConfigureAwait(false);
        }

        private async Task ReadAgentTimingStatus()
        {
            try
            {
                string status = await File.ReadAllTextAsync(GetAgentTimingStatusFilePath()).ConfigureAwait(false);
                string[] statusSplit = status.Split(',');

                mWasResetOnMidnightAlreadyPerformed = bool.Parse(statusSplit[0]);

                if (!bool.Parse(statusSplit[1]))
                    UpdateTasksFromInputFileHandler.SetDone();

                if (!bool.Parse(statusSplit[2]))
                    TodaysFutureReportHandler.SetDone();

                if (!bool.Parse(statusSplit[3]))
                    WeeklySummarySentHandler.SetDone();

                if (!bool.Parse(statusSplit[4]))
                    DailySummarySentTimingHandler.SetDone();
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Failed to read agent timing status");
            }
        }

        private string GetAgentTimingStatusFilePath()
        {
            return Path.Combine(mOptions.CurrentValue.DatabaseDirectoryPath, TimingStatusFileName);
        }
    }
}