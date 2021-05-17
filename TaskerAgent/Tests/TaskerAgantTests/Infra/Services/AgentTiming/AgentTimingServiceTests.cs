using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.AgentTiming;
using Xunit;

namespace TaskerAgantTests.Infra.Services.AgentTiming
{
    public class AgentTimingServiceTests
    {
        private const string TimingStatusFileName = "timing_status";

        [Fact]
        public async Task DailySummarySentTimingHandler_SetDone_TimingStatusIsUpdated()
        {
            string tempDatabaseDirectory = Directory.CreateDirectory(Path.GetRandomFileName()).FullName;

            try
            {
                IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
                configuration.CurrentValue.DatabaseDirectoryPath = tempDatabaseDirectory;

                AgentTimingService agentTimingService = new AgentTimingService(configuration, NullLogger<AgentTimingService>.Instance);
                agentTimingService.DailySummarySentTimingHandler.SetDone();

                string timingStatusText = await File.ReadAllTextAsync(Path.Combine(tempDatabaseDirectory, TimingStatusFileName)).ConfigureAwait(false);
                Assert.Equal("False,True,True,True,False", timingStatusText);
            }
            finally
            {
                Directory.Delete(tempDatabaseDirectory, recursive: true);
            }
        }

        [Fact]
        public async Task AgentTimingService_Ctor_TimingStatusIsUpdated()
        {
            string tempDatabaseDirectory = Directory.CreateDirectory(Path.GetRandomFileName()).FullName;

            try
            {
                await File.WriteAllTextAsync(Path.Combine(tempDatabaseDirectory, TimingStatusFileName), "False,True,True,True,False").ConfigureAwait(false);

                IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
                configuration.CurrentValue.DatabaseDirectoryPath = tempDatabaseDirectory;

                AgentTimingService agentTimingService = new AgentTimingService(configuration, NullLogger<AgentTimingService>.Instance);
                Assert.False(agentTimingService.DailySummarySentTimingHandler.ShouldDo);
            }
            finally
            {
                Directory.Delete(tempDatabaseDirectory, recursive: true);
            }
        }
    }
}