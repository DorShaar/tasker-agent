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
using TaskerAgent.App.Services.Email;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra.Services.Email
{
    public class EmailService : IEmailService
    {
        private const string ApplicationName = "TaskerAgent";
        private const string StmpGmailAddress = "smtp.gmail.com";
        private const string UserId = "me";
        private const string TaskerAgentLable = "Label_2783741690411084443";
        private const string UnreadMessageLable = "UNREAD";

        private readonly string[] mScopes = new[] { "https://mail.google.com/", };
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<EmailService> mLogger;

        private bool mDisposed;

        private SmtpServer mSmtpServer;
        private SmtpClient mSmtpClient;
        private bool mIsConnected;
        private GmailService mGmailService;

        public EmailService(IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
                ILogger<EmailService> logger)
        {
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Connect()
        {
            string accessToken = await File.ReadAllTextAsync(
                mTaskerAgentOptions.CurrentValue.AccessTokenPath).ConfigureAwait(false);

            mSmtpServer = new SmtpServer(StmpGmailAddress)
            {
                ConnectType = SmtpConnectType.ConnectSSLAuto,
                Port = 587, // Using 587 port, you can also use 465 port
                AuthType = SmtpAuthType.XOAUTH2,
                User = mTaskerAgentOptions.CurrentValue.EmailToNotify,
                Password = accessToken
            };

            mSmtpClient = new SmtpClient();
            mIsConnected = true;

            mLogger.LogInformation($"Connected to smtp server {StmpGmailAddress}");

            mGmailService = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = await GetUserCredential().ConfigureAwait(false),
                ApplicationName = ApplicationName,
            });

            mLogger.LogInformation("Connected to Gmail service");
        }

        private async Task<UserCredential> GetUserCredential()
        {
            using Stream stream =
                new FileStream(mTaskerAgentOptions.CurrentValue.CredentialsPath, FileMode.Open, FileAccess.Read);

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

        public Task SendMessage(string subject, string message)
        {
            if (!mIsConnected)
                throw new InvalidOperationException("Could not send message. Please connect first");

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

                mLogger.LogInformation($"Email {subject} has been submitted to server successfully!");
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Could not send email {subject} to the server");
            }

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<MessageInfo>> ReadMessages()
        {
            if (!mIsConnected)
                throw new InvalidOperationException("Could not read messagess. Please connect first");

            List<MessageInfo> messages = new List<MessageInfo>();

            var messagesListRequest = mGmailService.Users.Messages.List(UserId);
            messagesListRequest.LabelIds = new List<string>() { TaskerAgentLable, UnreadMessageLable };

            ListMessagesResponse listMessagesResponse = await messagesListRequest.ExecuteAsync().ConfigureAwait(false);

            if (listMessagesResponse.Messages == null)
                return messages;

            foreach (Message message in listMessagesResponse.Messages)
            {
                var messageGetRequest = mGmailService.Users.Messages.Get(UserId, message.Id);
                var messageResponse = await messageGetRequest.ExecuteAsync().ConfigureAwait(false);

                if (messageResponse.Payload.Body.Data != null)
                    messages.Add(new MessageInfo(message.Id, ConvertFromBase64(messageResponse.Payload.Body.Data)));
                else
                    messages.Add(new MessageInfo(message.Id, ConvertFromBase64(messageResponse.Payload.Parts[0].Body.Data)));
            }

            return messages;
        }

        private string ConvertFromBase64(string base64Text)
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(base64Text);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public async Task MarkMessageAsRead(string messageId)
        {
            if (!mIsConnected)
                throw new InvalidOperationException("Could not mark messages as read. Please connect first");

            ModifyMessageRequest modifyMessageRequest = new ModifyMessageRequest()
            {
                RemoveLabelIds = new List<string> { UnreadMessageLable }
            };

            var messageModifyRequest = mGmailService.Users.Messages.Modify(modifyMessageRequest, UserId, messageId);
            var messageResponse = await messageModifyRequest.ExecuteAsync().ConfigureAwait(false);

            if (messageResponse.LabelIds.Contains(UnreadMessageLable))
                mLogger.LogWarning($"Failed to remove label {UnreadMessageLable} from message id {messageId}");
            else
                mLogger.LogInformation($"Marked message id {messageId} as read");
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
                mGmailService.Dispose();

            mDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}