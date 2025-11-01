namespace HashLib7
{
    public interface IAsyncManager
    {
        void ExecuteAsync(string path, int numThreads);
        void Abort();
        void Suspend();
        void Resume();

    }
}