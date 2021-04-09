using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskerAgent.Infra.Services.Email;

namespace TaskerAgent.App.Services.Email
{
    public interface IEmailService: IDisposable, IAsyncDisposable
    {
        Task Connect();
        Task SendMessage(string subject, string message);
        Task<IEnumerable<MessageInfo>> ReadMessages();
        Task MarkMessageAsRead(string messageId);
    }
}