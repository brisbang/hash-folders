namespace HashLib7
{
    public class TaskWait(AsyncManager parent) : Task(parent, TaskStatusEnum.tseWait)
    {
        public override string Verb => "Wait";

        public override string Target => "";

        public override void Execute()
        {
            System.Threading.Thread.Sleep(500);
        }
    }
}