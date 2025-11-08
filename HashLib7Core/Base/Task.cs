using System;

namespace HashLib7
{
    internal enum TaskStatusEnum
    {
        tseProcess,
        tseWait,
        tseFinished,
    }
    public abstract class Task
    {
        internal TaskStatusEnum Status;
        public AsyncManager Parent;
        internal Task(AsyncManager parent, TaskStatusEnum status)
        {
            Status = status;
            Parent = parent;            
        }


        public abstract void Execute();
        public virtual void RegisterCompleted() {}
        public abstract string Verb { get; }
        public abstract string Target { get; }
    }
}