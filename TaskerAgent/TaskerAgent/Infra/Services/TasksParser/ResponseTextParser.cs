using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.App.Persistence.Repositories;
using TaskerAgent.Domain.RepetitiveTasks;
using TaskerAgent.Infra.Consts;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra.Services.TasksParser
{
    public class ResponseTextParser
    {
        private readonly string mReplySeparator;
        private readonly IDbRepository<ITasksGroup> mTasksGroupRepository;
        private readonly ILogger<ResponseTextParser> mLogger;

        public ResponseTextParser(IDbRepository<ITasksGroup> tasksGroupRepository,
            IOptionsMonitor<TaskerAgentConfiguration> options,
            ILogger<ResponseTextParser> logger)
        {
            mTasksGroupRepository = tasksGroupRepository ?? throw new ArgumentNullException(nameof(tasksGroupRepository));

            if(options == null)
                throw new ArgumentNullException(nameof(options));

            mReplySeparator = $"<{options.CurrentValue.EmailToNotify}>";

            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ITasksGroup> ParseResponse(string responseText)
        {
            string responseTextWithoutOriginalMessageContent = GetMailTextWithoutOriginalMessageContent(responseText);
            (string tasksText, string freeText) = SplitMessageText(responseTextWithoutOriginalMessageContent);

            ITasksGroup tasksGroup = await ParseTasks(tasksText).ConfigureAwait(false);

            return tasksGroup;
        }

        private string GetMailTextWithoutOriginalMessageContent(string responseText)
        {
            int separatorPosition = responseText.IndexOf(mReplySeparator);

            if (separatorPosition <= 0)
                return responseText;

            string textUntilSeparator = responseText.Substring(0, separatorPosition);
            int startOfOriginalMessageText = textUntilSeparator.LastIndexOf(AppConsts.EmailMessageNewLine);

            return textUntilSeparator.Substring(0, startOfOriginalMessageText);
        }

        private (string tasksText, string freeText) SplitMessageText(string responseText)
        {
            string tasksText;
            string freeText = string.Empty;

            int seperationIndex = responseText.IndexOf(AppConsts.EmailMessageNewLine + AppConsts.EmailMessageNewLine);

            if (seperationIndex < 1)
            {
                tasksText = responseText;
                return (responseText, string.Empty);
            }

            tasksText = responseText.Substring(0, seperationIndex);
            freeText = responseText.Substring(seperationIndex).Trim();

            return (tasksText, freeText);
        }

        private async Task<ITasksGroup> ParseTasks(string responseText)
        {
            string[] stringTasks = responseText.Split(AppConsts.EmailMessageNewLine);
            string date = stringTasks[1].Split("- ")[1].Replace(":", string.Empty);
            ITasksGroup tasksGroup = await mTasksGroupRepository.FindAsync(date).ConfigureAwait(false);

            if (tasksGroup == null)
            {
                mLogger.LogError("Failed to update according to given message");
                return null;
            }

            IEnumerable<IWorkTask> tasks = tasksGroup.GetAllTasks();
            foreach (string messagePart in stringTasks.Skip(2))
            {
                UpdateTask(tasks, messagePart);
            }

            return tasksGroup;
        }

        private void UpdateTask(IEnumerable<IWorkTask> tasks, string messagePart)
        {
            if (string.IsNullOrWhiteSpace(messagePart))
                return;

            string[] subMessageParts = messagePart.Split(".");
            string description = subMessageParts[0];
            IWorkTask task = tasks.FirstOrDefault(task => task.Description.Equals(description, StringComparison.OrdinalIgnoreCase));

            if (task == null || !(task is GeneralRepetitiveMeasureableTask repetitiveMeasureableTask))
            {
                mLogger.LogWarning($"Could not find task {description}");
                return;
            }

            string actualString = subMessageParts[2]
                .Replace(".", string.Empty)
                .Replace("actual:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            repetitiveMeasureableTask.Actual = Convert.ToInt32(actualString);
        }
    }
}