using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace LambdaThrows
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("before");

            var monitor = new TaskMonitor((o, e) =>
            {
                Console.WriteLine($"Exception in task caught {(e as MonitorException)?.Exception.Message}\n\n");
            })
            {
                Task.Run(() => Console.WriteLine("The first thingy")),
                Task.Run(() => throw new Exception("fuck you 1")),
                Task.Run(() => { Task.Delay(1000); Console.WriteLine("The middle thingy"); }),
                Task.Run(() => throw new Exception("fuck you 2")),
                Task.Run(() => Console.WriteLine("The last thingy")),
            };

            monitor.Add(
                Task.Run(() =>
                    {
                        Task.Delay(1000);
                        monitor.Add(Task.Run(() => Console.WriteLine("SubTask!")));
                    })
                );

            while (!monitor.Empty()) Task.Delay(100);

            Console.WriteLine("after");
        }
    }

    class MonitorException : EventArgs
    {
        public Exception Exception;
        public MonitorException(Exception exception)
        {
            Exception = exception;
        }
    }

    class TaskMonitor : ObservableCollection<Task>
    {
        Task monitor = null;

        public EventHandler Exception;
        public Boolean Empty() => monitor == null;

        public TaskMonitor(EventHandler exceptionHandler)
        {
            Exception = exceptionHandler;

            CollectionChanged += (o, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add && monitor == null)
                {
                    monitor = Task.Run(() =>
                    {
                        while (this.Any())
                        {
                            var finished = Task.WaitAny(this.ToArray());

                            if (this[finished].IsFaulted)
                                Exception?.Invoke(this, new MonitorException(this[finished].Exception));

                            RemoveAt(finished);
                        }
                        monitor = null;
                    });
                }
            };
        }
    }
}
