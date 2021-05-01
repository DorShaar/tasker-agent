using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services;
using TaskerAgent.Infra.Services.AgentTiming;
using Timer = System.Timers.Timer;

namespace TaskerAgent.Infra.HostedServices
{
    internal class TaskerAgentHostedService : IHostedService, IDisposable, IAsyncDisposable
    {
        private readonly TaskerAgentService mTaskerAgentService;
        private readonly AgentTimingService mAgentTimingService;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerOptions;
        private readonly ILogger<TaskerAgentHostedService> mLogger;

        private bool mDisposed;
        private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(1, 1);
        private Timer mNotifierTimer;

        public TaskerAgentHostedService(TaskerAgentService taskerAgentService,
            AgentTimingService agentTimingService,
            IOptionsMonitor<TaskerAgentConfiguration> options,
            ILogger<TaskerAgentHostedService> logger)
        {
            mTaskerAgentService = taskerAgentService ?? throw new ArgumentNullException(nameof(taskerAgentService));
            mAgentTimingService = agentTimingService ?? throw new ArgumentNullException(nameof(agentTimingService));
            mTaskerOptions = options ?? throw new ArgumentNullException(nameof(options));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            mLogger.LogDebug("Initializing tasker agent with interval of " +
                $"{mTaskerOptions.CurrentValue.NotifierInterval}");

            mNotifierTimer = new Timer
            {
                Interval = mTaskerOptions.CurrentValue.NotifierInterval.TotalMilliseconds,
                Enabled = true,
            };

            mNotifierTimer.Elapsed += PerformAgentActions;

            return Task.CompletedTask;
        }

        private async void PerformAgentActions(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (await mSemaphore.WaitAsync(TimeSpan.FromMinutes(2)).ConfigureAwait(false))
            {
                mAgentTimingService.ResetOnMidnight(elapsedEventArgs.SignalTime);

                await UpdateTaskFromInputFile(elapsedEventArgs).ConfigureAwait(false);

                await CheckForUserDailyReportsAndSendSummaries().ConfigureAwait(false);

                await CheckForMissingDailyReport(elapsedEventArgs).ConfigureAwait(false);

                await SendTodaysFutureTasksReport().ConfigureAwait(false);
                await SendWeeklySummary(elapsedEventArgs).ConfigureAwait(false);

                mSemaphore.Release();
            }
        }

        private async Task UpdateTaskFromInputFile(ElapsedEventArgs elapsedEventArgs)
        {
            if (mAgentTimingService.UpdateTasksFromInputFileHandler.ShouldDo)
            {
                await mTaskerAgentService.UpdateRepetitiveTasksFromInputFile().ConfigureAwait(false);
                mAgentTimingService.UpdateTasksFromInputFileHandler.SetDone();
            }
            else
            {
                mLogger.LogDebug($"Should not updated repetitive tasks yet {elapsedEventArgs.SignalTime.TimeOfDay}");
            }
        }

        private async Task CheckForMissingDailyReport(ElapsedEventArgs elapsedEventArgs)
        {
            if (mAgentTimingService.DailySummarySentTimingHandler.ShouldCheckIfDailySummaryWasSent(elapsedEventArgs.SignalTime))
            {
                if (!mAgentTimingService.DailySummarySentTimingHandler.IsContainMissingDate(elapsedEventArgs.SignalTime))
                {
                    mLogger.LogDebug($"Date {elapsedEventArgs.SignalTime.Date} was already reported back");
                    return;
                }

                await mTaskerAgentService.SendMissingReportMessage(elapsedEventArgs.SignalTime).ConfigureAwait(false);
            }
            else
            {
                mLogger.LogDebug($"Should not check if daily summary was sent yet {elapsedEventArgs.SignalTime.Date}");
            }
        }

        private async Task SendTodaysFutureTasksReport()
        {
            if (mAgentTimingService.TodaysFutureReportHandler.ShouldDo &&
                await mTaskerAgentService.SendTodaysFutureTasksReport().ConfigureAwait(false))
            {
                mAgentTimingService.TodaysFutureReportHandler.SetDone();
            }
        }

        private async Task SendWeeklySummary(ElapsedEventArgs elapsedEventArgs)
        {
            if (mAgentTimingService.ShouldSendWeeklySummary(elapsedEventArgs.SignalTime))
            {
                await mTaskerAgentService.SendWeeklySummary(elapsedEventArgs.SignalTime).ConfigureAwait(false);
                await mTaskerAgentService.SendThisWeekFutureTasksReport().ConfigureAwait(false);
                mAgentTimingService.WeeklySummarySentHandler.SetDone();
            }
            else
            {
                mLogger.LogDebug($"Should not send weekly summary yet {elapsedEventArgs.SignalTime.TimeOfDay}");
            }
        }

        private async Task CheckForUserDailyReportsAndSendSummaries()
        {
            IEnumerable<DateTime> datesGivenFeedbackByUser =
                await mTaskerAgentService.CheckForUserFeedbacks().ConfigureAwait(false);

            foreach (DateTime dateTime in datesGivenFeedbackByUser)
            {
                if (await mTaskerAgentService.SendDailySummary(dateTime).ConfigureAwait(false))
                    mAgentTimingService.DailySummarySentTimingHandler.SetDone(dateTime);

                mAgentTimingService.SignalDateGivenFeedbackByUser(dateTime);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            mLogger.LogDebug("Stopping timer");
            mNotifierTimer?.Stop();

            return Task.CompletedTask;
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
                mNotifierTimer?.Dispose();
                mAgentTimingService.Dispose();
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