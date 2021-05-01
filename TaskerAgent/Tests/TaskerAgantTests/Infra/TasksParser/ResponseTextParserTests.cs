using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.Infra.Options.Configurations;
using TaskerAgent.Infra.Services.TasksParser;
using Xunit;

namespace TaskerAgantTests.Infra.TasksParser
{
    public class ResponseTextParserTests
    {
        [Fact]
        public async Task ParseResponse_ParsedAsExpected()
        {
            const string messageResponse = @"Today's Tasks:
Saturday - 01/05/2021:
Drink Water. Expected: 2 Liters. ACtual: 5
Exercise. Expected: 3 Occurrences. Actual: 3
Sleep hours. Expected: 7 Hours. ACtual: 5
Eat bamba. Expected: 2 Occurrences. ACtual 6

Start of story

bla bla


blaaaa


‫בתאריך שבת, 1 במאי 2021 ב-13:20 מאת <‪dordatas@gmail.com‬‏>:‬

> Today's Tasks:
> Saturday - 01/05/2021:
> Drink Water. Expected: 2 Liters.
> Exercise. Expected: 3 Occurrences.
> Sleep hours. Expected: 7 Hours.
> Eat bamba. Expected: 2 Occurrences.
>
";

            IOptionsMonitor<TaskerAgentConfiguration> options = A.Dummy<IOptionsMonitor<TaskerAgentConfiguration>>();
            options.CurrentValue.EmailToNotify = "dordatas@gmail.com‬‏";

            ResponseTextParser responseTextParser = new ResponseTextParser(
                A.Dummy<IDbRepository<ITasksGroup>>(),
                options,
                NullLogger<ResponseTextParser>.Instance);

            ITasksGroup tasksGroup = await responseTextParser.ParseResponse(messageResponse).ConfigureAwait(false);
        }
    }
}