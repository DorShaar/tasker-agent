using System;

namespace TaskerAgent.Infra.Options.Configurations
{
    public class TaskerAgentConfiguration
    {
        public string DatabaseDirectoryPath { get; set; }
        public string InputFilePath { get; set; }
        public TimeSpan NotifierInterval { get; set; } = TimeSpan.FromHours(1);
        public int DaysToKeepForward { get; set; } = 40;
    }
}