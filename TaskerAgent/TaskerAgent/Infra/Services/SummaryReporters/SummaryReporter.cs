﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaskData.TasksGroups;
using TaskData.WorkTasks;
using TaskerAgent.Domain.RepetitiveTasks;

namespace TaskerAgent.Infra.Services.SummaryReporters
{
    public class SummaryReporter
    {
        private readonly ILogger<SummaryReporter> mLogger;

        public SummaryReporter(ILogger<SummaryReporter> logger)
        {
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string CreateTodaysFutureTasksReport(ITasksGroup tasksGroup)
        {
            StringBuilder reportBuilder = new StringBuilder();

            reportBuilder.AppendLine("Today's Tasks:");

            BuildFutureTasksReportForGivenDay(reportBuilder, tasksGroup);

            return reportBuilder.ToString();
        }

        public string CreateThisWeekFutureTasksReport(IEnumerable<ITasksGroup> tasksGroups)
        {
            StringBuilder reportBuilder = new StringBuilder();

            reportBuilder.AppendLine("This Week's Tasks:");

            foreach (ITasksGroup tasksGroup in tasksGroups.OrderBy(group => DateTime.Parse(group.Name)))
            {
                BuildFutureTasksReportForGivenDay(reportBuilder, tasksGroup);
                reportBuilder.AppendLine();
            }

            return reportBuilder.ToString();
        }

        private void BuildFutureTasksReportForGivenDay(StringBuilder reportBuilder, ITasksGroup tasksGroup)
        {
            DateTime dateTime = DateTime.Parse(tasksGroup.Name);

            reportBuilder.Append(dateTime.DayOfWeek).Append(" - ").Append(tasksGroup.Name).AppendLine(":");

            foreach (IWorkTask task in tasksGroup.GetAllTasks())
            {
                if (!(task is GeneralRepetitiveMeasureableTask repititiveTask))
                {
                    mLogger.LogError($"Task {task.ID} {task.Description} is not of type {nameof(GeneralRepetitiveMeasureableTask)}");
                    return;
                }

                reportBuilder.Append(repititiveTask.Description)
                .Append(". Expected: ")
                .Append(repititiveTask.Expected)
                .AppendLine(".");
            }
        }

        public string CreateDailySummaryReport(ITasksGroup tasksGroup)
        {
            StringBuilder reportBuilder = new StringBuilder();

            reportBuilder.AppendLine("Daily Summary Report:");

            BuildReportForDay(reportBuilder, tasksGroup);

            return reportBuilder.ToString();
        }

        public string CreateWeeklySummaryReport(List<ITasksGroup> tasksGroups)
        {
            StringBuilder reportBuilder = new StringBuilder();

            reportBuilder.AppendLine("Weekly Summary Report: ");

            foreach (ITasksGroup tasksGroup in tasksGroups)
            {
                BuildReportForDay(reportBuilder, tasksGroup);
                reportBuilder.AppendLine();
                reportBuilder.AppendLine();
            }

            return reportBuilder.ToString();
        }

        private void BuildReportForDay(StringBuilder reportBuilder, ITasksGroup tasksGroup)
        {
            reportBuilder.Append("Tasks Status - ").Append(tasksGroup.Name).AppendLine(":");
            int totalScore = 0;

            foreach (IWorkTask task in tasksGroup.GetAllTasks())
            {
                if (!(task is GeneralRepetitiveMeasureableTask repititiveTask))
                {
                    mLogger.LogError($"Task {task.ID} {task.Description} is not of type {nameof(GeneralRepetitiveMeasureableTask)}");
                    return;
                }

                float completePercentage = (float)repititiveTask.Actual / repititiveTask.Expected * 100;

                reportBuilder.Append(repititiveTask.Description)
                .Append(": ")
                .Append(repititiveTask.Actual)
                .Append('/')
                .Append(repititiveTask.Expected)
                .Append(". (")
                .Append(completePercentage)
                .AppendLine("%)");

                totalScore += repititiveTask.Score * repititiveTask.Actual;
            }

            reportBuilder.AppendLine()
                .Append("Total score: ")
                .Append(totalScore);
        }
    }
}