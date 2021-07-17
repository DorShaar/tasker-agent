using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskData.OperationResults;
using TaskData.TasksGroups;
using TaskData.TasksGroups.Producers;
using TaskData.WorkTasks;
using TaskData.WorkTasks.Producers;
using TaskerAgent.App.TasksProducers;
using TaskerAgent.Domain;
using TaskerAgent.Domain.RepetitiveTasks.RepetitiveMeasureableTasks;
using TaskerAgent.Infra.Options.Configurations;
using Triangle;

namespace TaskerAgent.Infra.Services.TasksParser
{
    public class FileTasksParser
    {
        private const string WhysDelimeter = "YYY";

        private readonly ITasksGroupFactory mTaskGroupFactory;
        private readonly ITasksProducerFactory mTasksProducerFactory;
        private readonly ITasksGroupProducer mTasksGroupProducer;
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<FileTasksParser> mLogger;

        public FileTasksParser(ITasksGroupFactory taskGroupFactory,
            ITasksProducerFactory tasksProducerFactory,
            ITasksGroupProducer tasksGroupProducer,
            IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
            ILogger<FileTasksParser> logger)
        {
            mTaskGroupFactory = taskGroupFactory ?? throw new ArgumentNullException(nameof(taskGroupFactory));
            mTasksProducerFactory = tasksProducerFactory ?? throw new ArgumentNullException(nameof(tasksProducerFactory));
            mTasksGroupProducer = tasksGroupProducer ?? throw new ArgumentNullException(nameof(tasksGroupProducer));
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Reads tasks from input file and returns List of groups.
        /// Each group contains tasks, the first task is the why task, the rest are its sub tasks.
        /// </summary>
        public async Task<IEnumerable<ITasksGroup>> ParseTasksToWhyGroups()
        {
            List<ITasksGroup> whyGroups = new List<ITasksGroup>();
            string text = await File.ReadAllTextAsync(mTaskerAgentOptions.CurrentValue.InputFilePath).ConfigureAwait(false);

            string[] whys = text.Split(WhysDelimeter);

            foreach (string whyTasks in whys[1..])
            {
                string[] whyLines = whyTasks.TrimStart('\r').TrimStart('\n').Split("\r\n");

                (string whyDescription, Frequency frequency) = ParseWhyLine(whyLines[0]);

                OperationResult<ITasksGroup> groupFromDataFileCreationResult = mTaskGroupFactory.CreateGroup(whyDescription, mTasksGroupProducer);
                if (!groupFromDataFileCreationResult.Success)
                {
                    mLogger.LogError($"Could not create group {whyDescription}");
                    return null;
                }

                ITasksGroup groupFromDataFile = groupFromDataFileCreationResult.Value;

                IWorkTaskProducer whyTasksProducer = mTasksProducerFactory.CreateWhyTasksProducer(frequency);
                OperationResult<IWorkTask> whyTaskCreationResult = mTaskGroupFactory.CreateTask(groupFromDataFile, whyDescription, whyTasksProducer);
                if (!whyTaskCreationResult.Success || whyTaskCreationResult.Value is not WhyMeasureableTask whyTask)
                {
                    mLogger.LogError($"Creation of why task {whyDescription} failed");
                    return null;
                }

                ParseSubTasksAndAddToGroup(groupFromDataFile, whyLines[1..], whyTask);
                whyGroups.Add(groupFromDataFile);
            }

            return whyGroups;
        }

        private static (string, Frequency) ParseWhyLine(string whyLine)
        {
            string[] parameters = whyLine.Trim(':').Split(',');
            string whyDescription = parameters[0];

            Frequency frequency = Frequency.NotDefined;

            if (parameters.Length > 1)
            {
                string frequencyString = parameters[1].Replace(" ", string.Empty).Replace("-", string.Empty);
                Enum.TryParse(frequencyString, ignoreCase: true, out frequency);
            }

            return (whyDescription, frequency);
        }

        private void ParseSubTasksAndAddToGroup(ITasksGroup taskGroup, string[] subTasks, WhyMeasureableTask whyTask)
        {
            foreach (string taskLine in subTasks)
            {
                if (string.IsNullOrWhiteSpace(taskLine))
                    continue;

                string[] parameters = taskLine.Trim(',').Split(',');

                CreateTaskFromParameters(taskGroup, parameters, whyTask);
            }
        }

        private void CreateTaskFromParameters(ITasksGroup taskGroup, string[] parameters, WhyMeasureableTask whyTask)
        {
            try
            {
                string taskDescription = parameters[0];

                ParsedComponents parseComponents = CreatedParsedComponentes(parameters);
                if (!parseComponents.WasParseSuccessfull)
                    return;

                IWorkTaskProducer taskProducer = GetWorkTaskProducer(parseComponents);

                if (taskProducer == null)
                {
                    mLogger.LogError($"Could not create task producer for task {taskDescription}");
                    return;
                }

                OperationResult<IWorkTask> createTaskResult = mTaskGroupFactory.CreateTask(taskGroup, taskDescription, taskProducer);

                if (!createTaskResult.Success)
                {
                    mLogger.LogError($"Could not create task {taskDescription}");
                    return;
                }

                UpdateWhyTaskMeasurement(whyTask, createTaskResult.Value);
            }
            catch (IndexOutOfRangeException ex)
            {
                LogError(ex, parameters);
            }
        }

        private ParsedComponents CreatedParsedComponentes(string[] parameters)
        {
            ParsedComponents parseComponents = new ParsedComponents();

            string taskDescription = parameters[0];

            if (!parseComponents.SetFrequency(parameters[1]))
            {
                if (parameters[1].Trim().Equals("one-time", StringComparison.OrdinalIgnoreCase))
                    return CreatedParsedComponentesForOneTimeTask(parameters[2]);

                mLogger.LogError($"Could not set frequency for task {taskDescription}");
                parseComponents.FailParse();
                return parseComponents;
            }

            if (!parseComponents.SetExpected(parameters[2]))
            {
                mLogger.LogError($"Could not set expected for task {taskDescription}");
                parseComponents.FailParse();
                return parseComponents;
            }

            if (!parseComponents.SetMeasureType(parameters[3]))
            {
                mLogger.LogError($"Could not set measure type for task {taskDescription}");
                parseComponents.FailParse();
                return parseComponents;
            }

            if (parameters.Length > 4)
            {
                if (parseComponents.Frequency == Frequency.Weekly)
                {
                    if (!parseComponents.SetOccurrenceDays(parameters[4..]))
                    {
                        mLogger.LogError($"Could not set OccurrenceDays for task {taskDescription}");
                        parseComponents.FailParse();
                        return parseComponents;
                    }
                }

                if (parseComponents.Frequency == Frequency.Monthly)
                {
                    if (!parseComponents.SetDaysOfMonth(parameters[4..]))
                    {
                        mLogger.LogError($"Could not set days of month for task {taskDescription}");
                        parseComponents.FailParse();
                        return parseComponents;
                    }
                }
            }

            return parseComponents;
        }

        private ParsedComponents CreatedParsedComponentesForOneTimeTask(string dateString)
        {
            ParsedComponents parseComponents = new ParsedComponents();

            if (!parseComponents.SetDueDateTime(dateString))
            {
                mLogger.LogError($"Could not set due date time with date {dateString}");
                parseComponents.FailParse();
                return parseComponents;
            }

            return parseComponents;
        }

        private static void UpdateWhyTaskMeasurement(IWorkTask whyTask, IWorkTask workSubTask)
        {
            whyTask.TaskMeasurement.Content.AddContent(workSubTask.ID);
            if (workSubTask.TaskMeasurement != null)
            {
                whyTask.TaskMeasurement.Time.StartTime = workSubTask.TaskMeasurement.Time.StartTime;
                whyTask.TaskMeasurement.Time.ExpectedDuration = TimeSpan.FromMinutes(5);
            }
        }

        private void LogError(IndexOutOfRangeException ex, string[] parameters)
        {
            switch (parameters.Length)
            {
                case 0:
                    mLogger.LogError(ex, "No parameters given");
                    break;

                case 1:
                    mLogger.LogError(ex, $"No frequency given for task {parameters[0]}");
                    break;

                case 2:
                    mLogger.LogError(ex, $"No expected given for task {parameters[0]}");
                    break;

                case 3:
                    mLogger.LogError(ex, $"No measurement type given for task {parameters[0]}");
                    break;

                default:
                    mLogger.LogError(ex, $"Failed to parse task {parameters[0]}");
                    break;
            }
        }

        private IWorkTaskProducer GetWorkTaskProducer(ParsedComponents parseComponents)
        {
            if (parseComponents.Frequency == Frequency.Daily)
            {
                return mTasksProducerFactory.CreateDailyProducer(
                    parseComponents.MeasureType, parseComponents.Expected, score: 1);
            }

            if (parseComponents.Frequency == Frequency.Weekly)
            {
                return mTasksProducerFactory.CreateWeeklyProducer(
                    parseComponents.MeasureType, parseComponents.OccurrenceDays, parseComponents.Expected, score: 1);
            }

            if (parseComponents.Frequency == Frequency.DuWeekly)
            {
                return mTasksProducerFactory.CreateDuWeeklyProducer(
                    parseComponents.MeasureType, parseComponents.OccurrenceDays, parseComponents.Expected, score: 1);
            }

            if (parseComponents.Frequency == Frequency.Monthly)
            {
                return mTasksProducerFactory.CreateMonthlyProducer(
                    parseComponents.MeasureType, parseComponents.DaysOfMonth, parseComponents.Expected, score: 1);
            }

            if (parseComponents.Frequency == Frequency.NotDefined)
            {
                return mTasksProducerFactory.CreateRegularTaskProducer(parseComponents.DueDateTime);
            }

            return null;
        }
    }
}