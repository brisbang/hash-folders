using System;

namespace HashLib7
{
    public class Worker
    {
        private System.Threading.Thread _thread = null;
        protected readonly AsyncManager Parent;
        private Task task = null;
        private bool enteredLoop = false;

        private string Activity
        {
            get
            {
                if (task == null)
                    return enteredLoop ? "Finished" : "Starting";
                return task.ToString();
            }
        }

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
            finally
            {
                Parent.ThreadIsFinished();
            }
        }


        protected void Main()
        {
            try
            {
                PathFormatted p = new(Parent.Path);
                bool finished = false;
                while (!finished)
                {
                    task = Parent.GetNextTask();
                    enteredLoop = true;
                    if (task == null)
                        finished = true;
                    else
                    {
                        try
                        {
                            task.Execute();
                        }
                        catch (Exception ex) { Console.Error.WriteLine(ex.ToString()); }
                        task.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                task.Dispose();
            }
        }
    }
}