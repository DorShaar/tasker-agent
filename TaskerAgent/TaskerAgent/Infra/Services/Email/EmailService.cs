using EASendMail;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra.Services.Email
{
    public class EmailService
    {
        private const string ApplicationName = "TaskerAgent";
        private const string StmpGmailAddress = "smtp.gmail.com";
        private const string TaskerAgentLable = "Label_2783741690411084443";

        private readonly string[] mScopes = new[] { "https://mail.google.com/", };
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<EmailService> mLogger;

        private readonly SmtpServer mSmtpServer;
        private readonly SmtpClient mSmtpClient;

        public EmailService(IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
                ILogger<EmailService> logger)
        {
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));

            string accessToken = File.ReadAllText(mTaskerAgentOptions.CurrentValue.AccessTokenPath);
            mSmtpServer = new SmtpServer(StmpGmailAddress)
            {
                ConnectType = SmtpConnectType.ConnectSSLAuto,
                Port = 587, // Using 587 port, you can also use 465 port
                AuthType = SmtpAuthType.XOAUTH2,
                User = mTaskerAgentOptions.CurrentValue.EmailToNotify,
                Password = accessToken
            };

            mSmtpClient = new SmtpClient();
        }

        public Task SendMessage(string subject, string message)
        {
            try
            {
                SmtpMail mail = new SmtpMail("TryIt")
                {
                    From = mTaskerAgentOptions.CurrentValue.EmailToNotify,
                    To = mTaskerAgentOptions.CurrentValue.EmailToNotify,
                    Subject = subject,
                    TextBody = message,
                };

                mSmtpClient.SendMail(mSmtpServer, mail);

                mLogger.LogDebug($"Email {subject} has been submitted to server successfully!");
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Could not send email {subject} to the server");
            }

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<string>> ReadMessages()
        {
            List<string> messages = new List<string>();

            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = await GetUserCredential().ConfigureAwait(false),
                ApplicationName = ApplicationName,
            });

            var messagesListRequest = service.Users.Messages.List("me");
            messagesListRequest.LabelIds = new List<string>() { TaskerAgentLable, "UNREAD" };

            ListMessagesResponse listMessagesResponse = await messagesListRequest.ExecuteAsync().ConfigureAwait(false);

            foreach (Message message in listMessagesResponse.Messages)
            {
                var messageGetRequest = service.Users.Messages.Get("me", message.Id);
                var messageResponse = await messageGetRequest.ExecuteAsync().ConfigureAwait(false);

                messages.Add(ConvertFromBase64(messageResponse.Payload.Body.Data));
            }

            return messages;
        }

        private async Task<UserCredential> GetUserCredential()
        {
            using FileStream stream = new FileStream(mTaskerAgentOptions.CurrentValue.CredentialsPath, FileMode.Open, FileAccess.Read);

            // The file token.json stores the user's access and refresh tokens, and is created
            // automatically when the authorization flow completes for the first time.
            const string credPath = "token.json";

            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                mScopes,
                "dordatas",
                CancellationToken.None,
                new FileDataStore(credPath, true)).ConfigureAwait(false);
        }

        private string ConvertFromBase64(string base64Text)
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(base64Text);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}