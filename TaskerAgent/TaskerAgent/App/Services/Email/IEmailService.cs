using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskerAgent.App.Services.Email
{
    public interface IEmailService
    {
        Task SendMessage(string subject, string message);
        Task<IEnumerable<string>> ReadMessages();
    }
}