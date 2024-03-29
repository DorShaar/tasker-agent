﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TaskerAgent.App.Services.Calendar;
using TaskerAgent.Domain;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra.Services.Calendar
{
    public class CalendarTaskerService : ICalendarService
    {
        private const string ApplicationName = "TaskerAgent";
        private const string CalendarId = "primary";

        private readonly string[] mScopes = new[] { CalendarService.Scope.Calendar };
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<CalendarTaskerService> mLogger;

        private bool mDisposed;

        private bool mIsConnected;
        private CalendarService mCalendarService;

        public CalendarTaskerService(IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
                ILogger<CalendarTaskerService> logger)
        {
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Connect()
        {
            mCalendarService = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = await GetUserCredential().ConfigureAwait(false),
                ApplicationName = ApplicationName,
            });

            mLogger.LogInformation("Connected to Google Calendar service");

            mIsConnected = true;
        }

        private async Task<UserCredential> GetUserCredential()
        {
            using Stream stream =
                new FileStream(mTaskerAgentOptions.CurrentValue.CredentialsPath, FileMode.Open, FileAccess.Read);

            // The file token.json stores the user's access and refresh tokens, and is created
            // automatically when the authorization flow completes for the first time.
            const string credPath = "token.json";

            GoogleClientSecrets clientSecrets = await GoogleClientSecrets.FromStreamAsync(stream).ConfigureAwait(false);

            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                mScopes,
                "dordatas",
                CancellationToken.None,
                new FileDataStore(credPath, true)).ConfigureAwait(false);
        }

        public async Task PullEvents()
        {
            if (!mIsConnected)
                throw new InvalidOperationException("Could not pull events. Please connect first");

            EventsResource.ListRequest request = mCalendarService.Events.List(CalendarId);
            request.TimeMin = DateTime.Now;
            request.TimeMax = DateTime.Now.AddDays(mTaskerAgentOptions.CurrentValue.DaysToKeepForward);
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            Events events = await request.ExecuteAsync().ConfigureAwait(false);
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (Event eventItem in events.Items)
                {
                    string when = eventItem.Start.DateTime.ToString();
                }
            }
        }

        public async Task PushEvent(string summary, DateTime start, DateTime end, Frequency frequency)
        {
            if (!mIsConnected)
                throw new InvalidOperationException("Could not push events. Please connect first");

            EventDateTime startEventDateTime = new EventDateTime
            {
                DateTime = start
            };

            EventDateTime endEventDateTime = new EventDateTime
            {
                DateTime = end
            };

            Event newEvent = new Event()
            {
                Summary = summary,
                Start = startEventDateTime,
                End = endEventDateTime,
                // Look for Recurrence Rule at https://tools.ietf.org/html/rfc5545#section-3.8.5
                Recurrence = new string[] { "RRULE:FREQ=DAILY;COUNT=2" },
            };

            EventsResource.InsertRequest request = mCalendarService.Events.Insert(newEvent, CalendarId);

            _ = await request.ExecuteAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (mDisposed)
                return;

            if (disposing)
                mCalendarService.Dispose();

            mDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}