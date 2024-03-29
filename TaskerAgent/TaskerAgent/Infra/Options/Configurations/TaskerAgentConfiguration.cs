﻿using System;

namespace TaskerAgent.Infra.Options.Configurations
{
    public class TaskerAgentConfiguration
    {
        public string UserName { get; set; }
        public string DatabaseDirectoryPath { get; set; }
        public string InputFilePath { get; set; }
        public TimeSpan NotifierInterval { get; set; } = TimeSpan.FromHours(1);
        public int TimeToNotify { get; set; } = 8;
        public int DaysToKeepForward { get; set; } = 40;
        public string EmailToNotify { get; set; }
        public string CredentialsPath { get; set; }
    }
}