using System;

namespace HashLib7
{
    public abstract class Worker
    {
        private System.Threading.Thread _thread = null;
        protected readonly AsyncManager Parent;
        protected abstract void Execute();


        public Worker(AsyncManager parent)
        {
            Parent = parent;
            _thread = new System.Threading.Thread(ExecuteInternal);
        }

        public void ExecuteAsync()
        {
            _thread.Start();
        }

        public void ExecuteInternal()
        {
            try
            {
                Parent.ThreadIsStarted();
            }
            catch { }
            finally
            {
                Parent.ThreadIsFinished();
            }
        }

        protected bool ShouldProcessNextTask()
        {
            while (true)
            {
                switch (Parent.State)
                {
                    case StateEnum.Stopped: return false; //Weird
                    case StateEnum.Aborting: return false;
                    case StateEnum.Suspended:
                        System.Threading.Thread.Sleep(500);
                        break;
                    case StateEnum.Running: return true;
                    default: throw new Exception("Unknown Parent.State" + Parent.State.ToString());
                }
            }
        }

    }
}