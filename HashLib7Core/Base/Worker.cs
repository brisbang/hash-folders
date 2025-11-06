using System;

namespace HashLib7
{
    public abstract class Worker
    {
        private System.Threading.Thread _thread = null;
        protected readonly AsyncManager Parent;
        protected abstract void Execute(Task task);


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
                Main();
            }
            catch { }
            finally
            {
                Parent.ThreadIsFinished();
            }
        }


        protected void Main()
        {
            const int pauseMs = 500;
            Task task = null;
            try
            {
                PathFormatted p = new(Parent.Path);
                bool finished = false;
                while (!finished && ShouldProcessNextTask())
                {
                    task = Parent.GetNextTask();
                    switch (task.status)
                    {
                        case TaskStatusEnum.tseProcess:
                            Execute(task);
                            TaskDispose(task);
                            break;
                        case TaskStatusEnum.tseWait:
                            System.Threading.Thread.Sleep(pauseMs);
                            break;
                        case TaskStatusEnum.tseFinished:
                            finished = true;
                            break;
                        default: throw new InvalidOperationException("Unknown ReportTaskEnum " + task.ToString());
                    }

                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                TaskDispose(task);
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


        private void TaskDispose(Task task)
        {
            try
            {
                switch (task.status)
                {
                    case TaskStatusEnum.tseProcess:
                        if (task.nextFile != null)
                            Parent.FileScanned(task.nextFile.filePath);
                        else
                            Parent.FolderScanned(task.nextFolder);
                        break;
                }
            }
            catch { }

        }
    }
}