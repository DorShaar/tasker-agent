using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.App.Services.Email;
using TaskerAgent.Domain.RepetitiveTasks;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services;
using TaskerAgent.Infra.Services.Email;
using Xunit;

namespace TaskerAgantTests.Infra.Services
{
    public class TaskerAgentServiceTests
    {
        private readonly IServiceCollection mServiceCollection;

        public TaskerAgentServiceTests()
        {
            mServiceCollection = new ServiceCollection();
            mServiceCollection.UseDI();

            TaskerAgentService service = mServiceCollection.BuildServiceProvider().GetRequiredService<TaskerAgentService>();
            service.UpdateRepetitiveTasks().ConfigureAwait(false);
        }

        [Fact]
        public async Task SendTodaysTasksReport_AsExpected()
        {
            IEmailService emailService = A.Fake<IEmailService>();
            mServiceCollection.AddSingleton(emailService);

            ServiceProvider serviceProvider = mServiceCollection.BuildServiceProvider();
            TaskerAgentService service = serviceProvider.GetRequiredService<TaskerAgentService>();

            await service.SendTodaysTasksReport().ConfigureAwait(false);

            A.CallTo(() => emailService.SendMessage(
                A<string>.Ignored,
                A<string>.That.Matches(report =>
                    report.Contains("Today's Tasks:", StringComparison.OrdinalIgnoreCase) &&
                    report.Contains("Drink Water.", StringComparison.OrdinalIgnoreCase) &&
                    report.Contains("Exercise.", StringComparison.OrdinalIgnoreCase) &&
                    report.Contains("Sleep hours.", StringComparison.OrdinalIgnoreCase),
                    "as expected")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task SendThisWeekTasksReport_AsExpected()
        {
            IEmailService emailService = A.Fake<IEmailService>();
            mServiceCollection.AddSingleton(emailService);

            ServiceProvider serviceProvider = mServiceCollection.BuildServiceProvider();
            TaskerAgentService service = serviceProvider.GetRequiredService<TaskerAgentService>();

            string thisWeekReport = string.Empty;
            A.CallTo(() => emailService.SendMessage(A<string>.Ignored, A<string>.Ignored))
                .Invokes(fakeObjectCall => thisWeekReport = fakeObjectCall.Arguments.Get<string>(1));

            await service.SendThisWeekTasksReport().ConfigureAwait(false);

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
            IEmailService emailService = A.Fake<IEmailService>();
            mServiceCollection.AddSingleton(emailService);

            ServiceProvider serviceProvider = mServiceCollection.BuildServiceProvider();
            TaskerAgentService service = serviceProvider.GetRequiredService<TaskerAgentService>();

            string report = string.Empty;
            A.CallTo(() => emailService.SendMessage(A<string>.Ignored, A<string>.Ignored))
                .Invokes(fakeObjectCall => report = fakeObjectCall.Arguments.Get<string>(1));

            await service.SendDailySummary(DateTime.Now).ConfigureAwait(false);

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
            IEmailService emailService = A.Fake<IEmailService>();
            mServiceCollection.AddSingleton(emailService);

            ServiceProvider serviceProvider = mServiceCollection.BuildServiceProvider();
            TaskerAgentService service = serviceProvider.GetRequiredService<TaskerAgentService>();

            string report = string.Empty;
            A.CallTo(() => emailService.SendMessage(A<string>.Ignored, A<string>.Ignored))
                .Invokes(fakeObjectCall => report = fakeObjectCall.Arguments.Get<string>(1));

            await service.SendWeeklySummary(DateTime.Now).ConfigureAwait(false);

            string[] splittedReport = report.Split("Tasks Status");

            Assert.True(splittedReport.Length == 8);
        }

        [Fact]
        public async Task FulfillReport_AsExpected()
        {
            const string message = @"Today's Tasks:
Saturday - 03/04/2021:
Drink Water. Expected: 2. Actual: 1.
Exercise. Expected: 3. actual: 1.
Sleep hours. Expected: 7. Actual:    5.
Eat bamba. Expected: 2.Actual: 6
";

            IEmailService emailService = A.Fake<IEmailService>();
            A.CallTo(() => emailService.ReadMessages()).Returns(new MessageInfo[] { new MessageInfo("id", message) });
            mServiceCollection.AddSingleton(emailService);

            ServiceProvider serviceProvider = mServiceCollection.BuildServiceProvider();
            TaskerAgentService service = serviceProvider.GetRequiredService<TaskerAgentService>();

            IDbRepository<ITasksGroup> realTasksGroupRepository = serviceProvider.GetRequiredService<IDbRepository<ITasksGroup>>();

            ITasksGroup returnedTasksGroup = null;

            await service.CheckForUpdates().ConfigureAwait(false);

            IDbRepository<ITasksGroup> fakeTasksGroupRepository = A.Fake<IDbRepository<ITasksGroup>>(x => x.Wrapping(realTasksGroupRepository));

            A.CallTo(() => fakeTasksGroupRepository.AddOrUpdateAsync(A<ITasksGroup>.That.Matches(group => group.Name == "03/04/2021")))
                .MustHaveHappenedOnceExactly();

            List<IWorkTask> tasks = returnedTasksGroup.GetAllTasks().ToList();

            GeneralRepetitiveMeasureableTask task1 = tasks[0] as GeneralRepetitiveMeasureableTask;
            Assert.True(task1.Description == "Drink Water" && task1.Actual == 1);

            GeneralRepetitiveMeasureableTask task2 = tasks[1] as GeneralRepetitiveMeasureableTask;
            Assert.True(task2.Description == "Exercise" && task2.Actual == 1);

            GeneralRepetitiveMeasureableTask task3 = tasks[2] as GeneralRepetitiveMeasureableTask;
            Assert.True(task3.Description == "Sleep hours" && task3.Actual == 5);

            GeneralRepetitiveMeasureableTask task4 = tasks[3] as GeneralRepetitiveMeasureableTask;
            Assert.True(task4.Description == "Eat bamba" && task4.Actual == 6);
        }
    }
}