using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskerAgent.App.Services.Calendar;
using TaskerAgent.Domain.Calendar;
using TaskerAgent.Infra.Extensions;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.TasksParser;
using TaskerAgent.Infra.Services.TasksUpdaters;
using Xunit;

namespace TaskerAgantTests.Infra.Services.TasksUpdaters
{
    public class TasksSynchronizerTests
    {
        private const string TestFilesDirectory = "TestFiles";
        private const string DatabaseTestFilesPath = "TaskerAgentDB";

        private readonly string mInputFileName = Path.Combine(TestFilesDirectory, "start_with_why.txt");
        private readonly IServiceCollection mServiceCollection;

        public TasksSynchronizerTests()
        {
            mServiceCollection = new ServiceCollection();
            mServiceCollection.UseDI();
        }

        [Fact]
        public async Task Synchronize_AsExpected()
        {
            IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.DatabaseDirectoryPath = DatabaseTestFilesPath;
            configuration.CurrentValue.InputFilePath = mInputFileName;
            mServiceCollection.AddSingleton(configuration);

            FileTasksParser fileTasksParser =
                mServiceCollection.BuildServiceProvider().GetRequiredService<FileTasksParser>();

            List<EventInfo> events = new List<EventInfo>()
            {
                new EventInfo("abc", "drink water", "status", DateTime.Now, DateTime.Now)
            };

            ICalendarService calendarService = A.Fake<ICalendarService>();
            A.CallTo(() => calendarService.PullEvents(A<DateTime>.Ignored, A<DateTime>.Ignored))
                .Returns(events);

            TasksSynchronizer tasksSynchronizer = new TasksSynchronizer(
                fileTasksParser,
                calendarService,
                configuration,
                NullLogger<TasksSynchronizer>.Instance);

            await tasksSynchronizer.Synchronize().ConfigureAwait(false);
        }
    }
}