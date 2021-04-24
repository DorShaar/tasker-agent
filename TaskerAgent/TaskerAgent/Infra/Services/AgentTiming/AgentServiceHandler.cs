namespace TaskerAgent.Infra.Services.AgentTiming
{
    public class AgentServiceHandler
    {
        protected bool mIsOperationAlreadyDone;

        public void SetDone()
        {
            mIsOperationAlreadyDone = true;
        }

        public void SetNotDone()
        {
            mIsOperationAlreadyDone = false;
        }

        public bool ShouldDo
        {
            get
            {
                return !mIsOperationAlreadyDone;
            }
        }
    }
}