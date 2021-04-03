namespace TaskerAgent.Infra.Services.Email
{
    public class MessageInfo
    {
        public string Id { get; }
        public string Body { get; }

        public MessageInfo(string id, string body)
        {
            Id = id;
            Body = body;
        }
    }
}