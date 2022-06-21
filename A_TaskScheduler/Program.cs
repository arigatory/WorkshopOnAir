using System.Collections.Concurrent;
using System.Threading.Channels;

namespace A_TaskSheduler;

public static class Program
{
    public static async Task Main()
    {
        var tts = new ThrottlerTaskScheduler(3);

        var tasks = new List<Task>();
        for (int i = 0; i < 30; i++)
        {
            int local = i;
            
            var actionTask = Task.Factory.StartNew(
                async () =>
                {
                    Console.WriteLine($"#{local} - {i} start on thread {Environment.CurrentManagedThreadId}");
                    
                    await Task.Delay(TimeSpan.FromSeconds(2));

                    Console.WriteLine($"#{local} - {i} start on thread {Environment.CurrentManagedThreadId}");
                },
                CancellationToken.None, TaskCreationOptions.None,   
                tts);
            tasks.Add(actionTask);
        }

        await Task.WhenAll(tasks);
    }

    public class BlockingQueue<T>
    {
        private readonly object _synLock = new object();
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly SemaphoreSlim _pool = new SemaphoreSlim(0, Int32.MaxValue);

        public void Enqueue(T item)
        {
            lock (_synLock)
            {
                _queue.Enqueue(item);

                _pool.Release();
            }
        }

        public T Dequeue()
        {
            _pool.Wait();

            lock (_synLock)
            {
                return _queue.Dequeue();
            }
        }

        public IEnumerable<T> ToArray()
        {
            lock (_synLock)
            {
                return _queue.ToArray();
            }
        }
    }

    public class ThrottlerTaskScheduler : TaskScheduler
    {
        private readonly BlockingQueue<Task> _tasksQueue = new BlockingQueue<Task>();

        public ThrottlerTaskScheduler(int workerCount)
        {
            var workers = new Task[workerCount];

            for (var i = 0; i < workerCount; i++)
            {
                workers[i] = Task.Factory.StartNew(TryExecuteTasks, TaskCreationOptions.LongRunning);
            }
        }

        private void TryExecuteTasks()
        {
            while (true)
            {
                Task task = _tasksQueue.Dequeue();

                TryExecuteTask(task);
            }
        }

        protected override IEnumerable<Task>? GetScheduledTasks()
        {
            return _tasksQueue.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            _tasksQueue.Enqueue(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }
    }
}