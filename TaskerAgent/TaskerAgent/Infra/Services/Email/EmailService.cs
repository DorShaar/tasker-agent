using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskerAgent.App.Services.Email;
using TaskerAgent.Domain.Email;
using TaskerAgent.Infra.Consts;
using TaskerAgent.Infra.Options.Configurations;

namespace TaskerAgent.Infra.Services.Email
{
    public class EmailService : IEmailService
    {
        private const string UserId = "me";
        private const string TaskerAgentLable = "Label_2783741690411084443";
        private const string UnreadMessageLable = "UNREAD";

        private readonly string[] mScopes = new[] { GmailService.ScopeConstants.MailGoogleCom };
        private readonly IOptionsMonitor<TaskerAgentConfiguration> mTaskerAgentOptions;
        private readonly ILogger<EmailService> mLogger;

        private bool mDisposed;

        private bool mIsConnected;
        private GmailService mGmailService;

        public EmailService(IOptionsMonitor<TaskerAgentConfiguration> taskerAgentOptions,
                ILogger<EmailService> logger)
        {
            mTaskerAgentOptions = taskerAgentOptions ?? throw new ArgumentNullException(nameof(taskerAgentOptions));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Connect()
        {
            mGmailService = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = await GetUserCredential().ConfigureAwait(false),
                ApplicationName = AppConsts.ApplicationName,
            });

            mLogger.LogInformation("Connected to Gmail service");

            mIsConnected = true;
            return true;
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

        public async Task<bool> SendMessage(string subject, string messageBody)
        {
            try
            {
                Message message = await CreateEmail(subject, messageBody).ConfigureAwait(false);

                await mGmailService.Users.Messages.Send(message, UserId).ExecuteAsync().ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Could not send message {subject}");
                return false;
            }
        }

        private async Task<Message> CreateEmail(string subject, string textBody)
        {
            MimeMessage mimeMessage = new MimeMessage
            {
                Subject = subject,
            };

            mimeMessage.To.Add(MailboxAddress.Parse(mTaskerAgentOptions.CurrentValue.EmailToNotify));
            mimeMessage.From.Add(MailboxAddress.Parse(mTaskerAgentOptions.CurrentValue.EmailToNotify));

            BodyBuilder bodyBuilder = new BodyBuilder
            {
                TextBody = textBody
            };

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using MemoryStream memoryStream = new MemoryStream();

            await mimeMessage.WriteToAsync(memoryStream).ConfigureAwait(false);

            return new Message
            {
                Raw = EncodeMessage(memoryStream.ToArray())
            };
        }

        private static string EncodeMessage(byte[] messageBytes)
        {
            return Convert.ToBase64String(messageBytes);
        }

        public async Task<IEnumerable<MessageInfo>> ReadMessages(bool shouldReadAll = false)
        {
            if (!mIsConnected)
                throw new InvalidOperationException("Could not read messagess. Please connect first");

            List<MessageInfo> messages = new List<MessageInfo>();

            var messagesListRequest = mGmailService.Users.Messages.List(UserId);
            messagesListRequest.LabelIds = BuildLabels(shouldReadAll);

            try
            {
                ListMessagesResponse listMessagesResponse = await messagesListRequest.ExecuteAsync().ConfigureAwait(false);

                if (listMessagesResponse.Messages == null)
                    return messages;

                foreach (Message message in listMessagesResponse.Messages)
                {
                    messages.Add(await GetMessageInfo(message).ConfigureAwait(false));
                }

                return messages;
            }
            catch (HttpRequestException ex)
            {
                mLogger.LogError(ex, "Could not read messages");
                return Array.Empty<MessageInfo>();
            }
        }

        private static List<string> BuildLabels(bool shouldReadAll)
        {
            List<string> labels = new List<string>() { TaskerAgentLable };

            if (!shouldReadAll)
            {
                labels.Add(UnreadMessageLable);
            }

            return labels;
        }

        private async Task<MessageInfo> GetMessageInfo(Message message)
        {
            var messageGetRequest = mGmailService.Users.Messages.Get(UserId, message.Id);
            Message messageResponse = await messageGetRequest.ExecuteAsync().ConfigureAwait(false);

            return CreateMessageInfoFromMessage(messageResponse);
        }

        private MessageInfo CreateMessageInfoFromMessage(Message message)
        {
            string messageBody = GetMessageBody(message);
            DateTime dateCreated = GetDateMessageCreated(messageBody);

            return new MessageInfo(message.Id,
                GetSubject(message),
                messageBody,
                dateCreated);
        }

        private static string GetMessageBody(Message message)
        {
            if (message.Payload.Body.Data != null)
                return ConvertFromBase64(message.Payload.Body.Data);

            return ConvertFromBase64(message.Payload.Parts[0].Body.Data);
        }

        private static DateTime GetDateMessageCreated(string messageBody)
        {
            string lineWithDate = messageBody.Split(AppConsts.EmailMessageNewLine)[1];
            string dateString = lineWithDate.Split("- ")[1].Replace(":", string.Empty);
            return DateTime.Parse(dateString);
        }

        private string GetSubject(Message message)
        {
            return GetHeader(message, "Subject");
        }

        private string GetHeader(Message message, string headerName)
        {
            foreach (MessagePartHeader header in message.Payload.Headers)
            {
                if (header.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                    return header.Value;
            }

            mLogger.LogWarning($"Could not find the header {headerName} of message id {message.Id}");
            return string.Empty;
        }

        private static string ConvertFromBase64(string base64UrlText)
        {
            string base64Text = base64UrlText.Replace('_', '/').Replace('-', '+');
            switch (base64UrlText.Length % 4)
            {
                case 2: base64Text += "=="; break;
                case 3: base64Text += "="; break;
            }

            byte[] base64EncodedBytes = Convert.FromBase64String(base64Text);
            return Encoding.UTF8.GetString(base64EncodedBytes);
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