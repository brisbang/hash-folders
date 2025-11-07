namespace HashLib7
{
    public class TaskFinished(AsyncManager parent) : Task(parent, TaskStatusEnum.tseFinished)
    {
        public override void RegisterCompleted()
        {
        }

        public override void Execute()
        {
        }

        public override string ToString()
        {
            return "Finished";
        }
    }
}