namespace HashLib7
{
    public class TaskWait(AsyncManager parent) : Task(parent, TaskStatusEnum.tseWait)
    {
        public override void RegisterCompleted()
        {
        }

        public override void Execute()
        {
            System.Threading.Thread.Sleep(500);
        }

        public override string ToString()
        {
            return "Waiting for work";
        }
    }
}