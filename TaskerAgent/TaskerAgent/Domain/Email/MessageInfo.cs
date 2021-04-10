using System;

namespace TaskerAgent.Infra.Services.Email
{
    public class MessageInfo
    {
        public string Id { get; }
        public string Body { get; }
        public DateTime DateCreated { get; }

        public MessageInfo(string id, string body, DateTime dateCreated)
        {
            Id = id;
            Body = body;
            DateCreated = dateCreated;
        }
    }
}