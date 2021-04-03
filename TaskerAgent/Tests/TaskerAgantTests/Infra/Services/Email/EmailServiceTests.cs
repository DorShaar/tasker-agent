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
        [Fact]
        public async Task SendMessage_AsExpected()
        {
            IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.EmailToNotify = "dordatas@gmail.com";
            configuration.CurrentValue.AccessTokenPath = @"C:\Users\dor.shaar.CORP\Desktop\accessToken.txt";

            EmailService emailService = new EmailService(configuration, NullLogger<EmailService>.Instance);
            await emailService.SendMessage("TestSubject", "test message").ConfigureAwait(false);
        }

        [Fact]
        public async Task ReadMessage_AsExpected()
        {
            IOptionsMonitor<TaskerAgentConfiguration> configuration = A.Fake<IOptionsMonitor<TaskerAgentConfiguration>>();
            configuration.CurrentValue.EmailToNotify = "dordatas@gmail.com";
            configuration.CurrentValue.AccessTokenPath = @"C:\Users\dor.shaar.CORP\Desktop\accessToken.txt";
            configuration.CurrentValue.CredentialsPath = @"C:\Users\dor.shaar.CORP\Desktop\client_secret_1097329801335-0csu27isvkhg9ba0jnjqkpnpe8r6h5ev.apps.googleusercontent.com.json";

            EmailService emailService = new EmailService(configuration, NullLogger<EmailService>.Instance);
            await emailService.ReadMessages().ConfigureAwait(false);
        }
    }
}