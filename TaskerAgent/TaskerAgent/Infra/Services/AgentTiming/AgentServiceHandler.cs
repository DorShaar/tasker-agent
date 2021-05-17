using System;
using System.Threading.Tasks;

namespace TaskerAgent.Infra.Services.AgentTiming
{
    public class AgentServiceHandler
    {
        private bool mIsOperationAlreadyDone;
        public event EventHandler UpdatePerformed;

        public void SetDone()
        {
            mIsOperationAlreadyDone = true;
            UpdatePerformed?.Invoke(this, null);
        }

        public void SetNotDone()
        {
            mIsOperationAlreadyDone = false;
            UpdatePerformed?.Invoke(this, null);
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