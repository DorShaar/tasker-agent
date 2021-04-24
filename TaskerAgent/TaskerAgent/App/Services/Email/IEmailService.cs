using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskerAgent.Domain.Email;

namespace TaskerAgent.App.Services.Email
{
    public interface IEmailService: IDisposable, IAsyncDisposable
    {
        Task Connect();
        Task<bool> SendMessage(string subject, string message);
        Task<IEnumerable<MessageInfo>> ReadMessages(bool shouldReadAll = false);
        Task MarkMessageAsRead(string messageId);
    }
}