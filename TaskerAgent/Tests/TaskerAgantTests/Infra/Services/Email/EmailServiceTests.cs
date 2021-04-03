using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.Email;
using Xunit;

namespace TaskerAgantTests.Infra.Services.Email
{
    public class EmailServiceTests
    {
        [Fact(Skip = "Requires real email. To be tested manually")]
        public async Task SendMessage_AsExpected()
        {
            IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.EmailToNotify = "dordatas@gmail.com";
            configuration.CurrentValue.AccessTokenPath = @"C:\Dor\Projects\tasker-agent\TaskerAgent\tokens\accessToken.txt";

            EmailService emailService = new EmailService(configuration, NullLogger<EmailService>.Instance);
            await emailService.Connect().ConfigureAwait(false);
            await emailService.SendMessage("TestSubject", "test message").ConfigureAwait(false);
        }

        [Fact(Skip = "Requires real email. To be tested manually")]
        public async Task ReadMessage_AsExpected()
        {
            IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.EmailToNotify = "dordatas@gmail.com";
            configuration.CurrentValue.AccessTokenPath = @"C:\Dor\Projects\tasker-agent\TaskerAgent\tokens\accessToken.txt";
            configuration.CurrentValue.CredentialsPath = @"C:\Dor\Projects\tasker-agent\TaskerAgent\tokens\client_secret.json";

            EmailService emailService = new EmailService(configuration, NullLogger<EmailService>.Instance);
            await emailService.ReadMessages().ConfigureAwait(false);
        }
    }
}