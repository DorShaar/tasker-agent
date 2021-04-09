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
        private readonly ILogger<TaskerAgentHostedService> mLogger;

        private bool mDisposed;
        private readonly TaskerAgentService mTaskerAgentService;
        private readonly AgentTimingService mAgentTimingService;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerOptions;
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
            mAgentTimingService.ResetOnMidnight(elapsedEventArgs.SignalTime);

            if (!mAgentTimingService.ShouldUpdate())
            {
                await mTaskerAgentService.UpdateRepetitiveTasks().ConfigureAwait(false);
                mAgentTimingService.SignalUpdatePerformed();
            }
            else
            {
                mLogger.LogDebug($"Should not updated repetitive tasks yet {elapsedEventArgs.SignalTime.TimeOfDay}");
            }

            if (mAgentTimingService.ShouldSendDailySummary(elapsedEventArgs.SignalTime))
            {
                await mTaskerAgentService.SendDailySummary(elapsedEventArgs.SignalTime).ConfigureAwait(false);
                await mTaskerAgentService.SendTodaysTasksReport().ConfigureAwait(false);
                mAgentTimingService.SignalDailySummaryPerformed();
            }
            else
            {
                mLogger.LogDebug($"Should not send daily summary yet {elapsedEventArgs.SignalTime.TimeOfDay}");
            }

            if (mAgentTimingService.ShouldSendWeeklySummary(elapsedEventArgs.SignalTime))
            {
                await mTaskerAgentService.SendWeeklySummary(elapsedEventArgs.SignalTime).ConfigureAwait(false);
                await mTaskerAgentService.SendThisWeekTasksReport().ConfigureAwait(false);
                mAgentTimingService.SignalWeeklySummaryPerformed();
            }
            else
            {
                mLogger.LogDebug($"Should not send weekly summary yet {elapsedEventArgs.SignalTime.TimeOfDay}");
            }

            IEnumerable<DateTime> datesGivenFeedbackByUser =
                await mTaskerAgentService.CheckForUpdates().ConfigureAwait(false);

            mAgentTimingService.SignalDatesGivenFeedbackByUser(datesGivenFeedbackByUser);
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
                mNotifierTimer?.Dispose();

            mDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}