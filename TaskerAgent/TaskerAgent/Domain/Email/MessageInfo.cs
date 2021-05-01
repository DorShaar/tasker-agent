using System;

namespace TaskerAgent.Domain.Email
{
    public class MessageInfo
    {
        public string Id { get; }
        public string Subject { get; }
        public string Body { get; }
        public DateTime DateCreated { get; }

        public MessageInfo(string id, string subject, string body, DateTime dateCreated)
        {
            Id = id;
            Subject = subject;
            Body = body;
            DateCreated = dateCreated;
        }
    }
}