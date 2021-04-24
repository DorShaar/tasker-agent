namespace TaskerAgent.Infra.Services.AgentTiming
{
    public class AgentServiceHandler
    {
        protected bool mStatus;

        public void SetOn()
        {
            mStatus = true;
        }

        public void SetOff()
        {
            mStatus = false;
        }

        public bool Value
        {
            get
            {
                return mStatus;
            }
        }
    }
}