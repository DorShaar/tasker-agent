//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Timers;

//namespace TaskerAgent.Infra.HostedServices
//{
//    internal class TaskerAgentHostedService : IHostedService, IDisposable, IAsyncDisposable
//    {
//        private readonly ILogger<TaskerAgentHostedService> mLogger;

//        private bool mDisposed;
//        //private readonly ITaskerAgentService mTaskerAgentService; // tODO
//        //private readonly IOptionsMonitor<TaskerConfiguration> mTaskerOptions; // tODO 
//        private Timer mNotifierTimer;

//        public TaskerNotifier(INotifierService notifierService, IOptionsMonitor<TaskerConfiguration> options,
//            ILogger<TaskerNotifier> logger)
//        {
//            mNotifierService = notifierService ?? throw new ArgumentNullException(nameof(notifierService));
//            mTaskerOptions = options ?? throw new ArgumentNullException(nameof(options));
//            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public Task StartAsync(CancellationToken cancellationToken)
//        {
//            mLogger.LogDebug("Initializing triangle notifier timer with interval of " +
//                $"{mTaskerOptions.CurrentValue.NotifierInterval}");
//            mTriangleNotifierTimer = new Timer
//            {
//                Interval = mTaskerOptions.CurrentValue.NotifierInterval.TotalMilliseconds,
//                Enabled = true,
//            };

//            mLogger.LogDebug("Initializing daily notifier timer with interval of " +
//                $"{mTaskerOptions.CurrentValue.SummaryEmailInterval}");
//            mDailyNotifierTimer = new Timer
//            {
//                Interval = mTaskerOptions.CurrentValue.SummaryEmailInterval.TotalMilliseconds,
//                Enabled = true,
//            };

//            mTriangleNotifierTimer.Elapsed += OnTriangleNotifierElapsed;
//            mDailyNotifierTimer.Elapsed += OnDailyNotifierElapsed;

//            return Task.CompletedTask;
//        }

//        private async void OnTriangleNotifierElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
//        {
//            await mNotifierService.NotifyTriangleTasks().ConfigureAwait(false);
//        }

//        private async void OnDailyNotifierElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
//        {
//            await mNotifierService.NotifySummary().ConfigureAwait(false);
//        }

//        public Task StopAsync(CancellationToken cancellationToken)
//        {
//            mLogger.LogDebug("Stopping timer");
//            mTriangleNotifierTimer?.Stop();

//            return Task.CompletedTask;
//        }

//        public void Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//        protected virtual void Dispose(bool disposing)
//        {
//            mLogger.LogDebug("Closing timer");

//            if (mDisposed)
//                return;

//            if (disposing)
//                mTriangleNotifierTimer?.Dispose();

//            mDisposed = true;
//        }

//        public ValueTask DisposeAsync()
//        {
//            Dispose();
//            return default;
//        }
//    }
//}