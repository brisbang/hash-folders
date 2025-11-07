namespace HashLib7
{
    public class TaskFinished(AsyncManager parent) : Task(parent, TaskStatusEnum.tseFinished)
    {
        public override string Verb => "Close";

        public override string Target => "";

        public override void RegisterCompleted()
        {
        }

        public override void Execute()
        {
        }

    }
}