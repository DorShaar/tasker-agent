using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Services;
using Xunit;

namespace TaskerAgantTests.Infra.Services
{
    public class TaskerAgentServiceTests
    {
        private readonly ServiceProvider mServiceProvider;

        public TaskerAgentServiceTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.UseDI();
            mServiceProvider = serviceCollection.BuildServiceProvider();

            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            service.UpdateRepetitiveTasks().ConfigureAwait(false);
        }

        [Fact]
        public async Task SendTodaysTasksReport_AsExpected()
        {
            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            string todaysReport = await service.SendTodaysTasksReport().ConfigureAwait(false);

            Assert.Contains("Today's Tasks:", todaysReport, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Drink Water.", todaysReport, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Exercise.", todaysReport, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Sleep hours.", todaysReport, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendThisWeekTasksReport_AsExpected()
        {
            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            string thisWeekReport = await service.SendThisWeekTasksReport().ConfigureAwait(false);

            string[] splittedReport = thisWeekReport.Split("Drink Water");
            Assert.True(splittedReport.Length == 8);

            splittedReport = thisWeekReport.Split("Exercise");
            Assert.True(splittedReport.Length == 8);

            splittedReport = thisWeekReport.Split("Sleep hours");
            Assert.True(splittedReport.Length == 8);

            splittedReport = thisWeekReport.Split("Floss");
            Assert.True(splittedReport.Length == 4);

            splittedReport = thisWeekReport.Split("Eat bamba");
            Assert.True(splittedReport.Length == 2);

            splittedReport = thisWeekReport.Split("Run");
            Assert.True(splittedReport.Length == 3);
        }

        [Fact]
        public async Task SendDailySummary_AsExpected()
        {
            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            string report = await service.SendDailySummary(DateTime.Now).ConfigureAwait(false);

            Assert.Contains("Daily Summary Report:", report);
            Assert.Contains("Tasks Status - ", report);
            Assert.Contains("Drink Water:", report);
            Assert.Contains("Exercise:", report);
            Assert.Contains("Sleep hours:", report);
            Assert.Contains("Total score:", report);
        }

        [Fact]
        public async Task SendWeeklySummary_AsExpected()
        {
            TaskerAgentService service = mServiceProvider.GetRequiredService<TaskerAgentService>();

            string report = await service.SendWeeklySummary(DateTime.Now).ConfigureAwait(false);

            string[] splittedReport = report.Split("Tasks Status");

            Assert.True(splittedReport.Length == 8);
        }
    }
}