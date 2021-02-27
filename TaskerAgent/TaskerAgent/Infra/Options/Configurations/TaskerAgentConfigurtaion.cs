using System;

namespace TaskerAgent.Infra.Options.Configurations
{
    public class TaskerAgentConfigurtaion
    {
        public string DatabaseDirectoryPath { get; set; }
        public string InputFilePath { get; set; }
        public TimeSpan NotifierInterval { get; set; }
    }
}