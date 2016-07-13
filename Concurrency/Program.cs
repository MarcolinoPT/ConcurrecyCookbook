namespace Concurrency
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    class Program
    {
        static void Main(string[] args)
        {
            Chapter2_AsyncBasic.AnsycDemo.Run();
        }
    }

    class AsyncProgramming
    {
        public static async Task DoSomethingAsync()
        {
            var val = 13;
            // Asynchronously wait 1 second.
            await Task.Delay(TimeSpan.FromSeconds(1)); // Same context thread
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false); // Is resumed on a thread-pool thread
            val *= 2;
            // Asynchronously wait 1 second.
            await Task.Delay(TimeSpan.FromSeconds(1)); // Same context thread
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false); // Is resumed on a thread-pool thread
            Trace.WriteLine(val);
        }
    }

    class Chapter2_AsyncBasic
    {
        public class Pausing_for_a_Period_of_Time
        {
            public static async Task<T> DelayResultAsync<T>(T result, TimeSpan delay)
            {
                await Task.Delay(delay);
                return result;
            }

            static async Task<string> DownloadStringWithRetries(string uri)
            {
                using (var client = new HttpClient())
                {
                    // Retry after 1 second, then after 2 seconds, then 4.

                    var nextDelay = TimeSpan.FromSeconds(1);
                    for (int i = 0; i != 3; ++i)
                    {
                        try
                        {
                            return await client.GetStringAsync(uri);
                        }
                        catch { }
                        await Task.Delay(nextDelay);
                        nextDelay = nextDelay + nextDelay;
                    }
                    // Try one last time, allowing the error to propogate.
                    return await client.GetStringAsync(uri);
                }
            }

            static async Task<string> DownloadStringWithTimeout(string uri)
            {
                using (var client = new HttpClient())
                {
                    var downloadTask = client.GetStringAsync(uri);
                    var timeoutTask = Task.Delay(3000);
                    var completedTask = await Task.WhenAny(downloadTask, timeoutTask);
                    if (completedTask == timeoutTask)
                        return null;
                    return await downloadTask;
                }
            }
        }

        public class Returning_Completed_Tasks
        {
            interface IMyAsyncInterface
            {
                Task<int> GetValueAsync();
            }
            class MySynchronousImplementation : IMyAsyncInterface
            {
                public Task<int> GetValueAsync()
                {
                    return Task.FromResult(13);
                }
            }

            static Task<T> NotImplementedAsync<T>()
            {
                var tcs = new TaskCompletionSource<T>();
                tcs.SetException(new NotImplementedException());
                return tcs.Task;
            }

            private static readonly Task<int> zeroTask = Task.FromResult(0);
            static Task<int> GetValueAsync()
            {
                return zeroTask;
            }
        }

        public class Reporting_Progress
        {
            static async Task MyMethodAsync(IProgress<double> progress = null)
            {
                var done = false;
                double percentComplete = 0;
                while (!done)
                {
                    // Does something
                    if (progress != null)
                        progress.Report(percentComplete);
                }
            }

            static async Task CallMyMethodAsync()
            {
                var progress = new Progress<double>();
                progress.ProgressChanged += (sender, args) =>
                {
                    // Does something
                };
                await MyMethodAsync(progress);
            }
        }

        public class Waiting_for_a_Set_of_Tasks_to_Complete
        {
            static async Task<string> DownloadAllAsync(IEnumerable<string> urls)
            {
                var httpClient = new HttpClient();
                // Define what we're going to do for each URL.
                var downloads = urls.Select(url => httpClient.GetStringAsync(url));
                // Note that no tasks have actually started yet
                // because the sequence is not evaluated.
                // Start all URLs downloading simultaneously.
                Task<string>[] downloadTasks = downloads.ToArray();
                // Now the tasks have all started.
                // Asynchronously wait for all downloads to complete.
                string[] htmlPages = await Task.WhenAll(downloadTasks);
                return string.Concat(htmlPages);
            }

            static async Task ThrowNotImplementedExceptionAsync()
            {
                throw new NotImplementedException();
            }
            static async Task ThrowInvalidOperationExceptionAsync()
            {
                throw new InvalidOperationException();
            }
            static async Task ObserveOneExceptionAsync()
            {
                var task1 = ThrowNotImplementedExceptionAsync();
                var task2 = ThrowInvalidOperationExceptionAsync();
                try
                {
                    await Task.WhenAll(task1, task2);
                }
                catch (Exception ex)
                {
                    // "ex" is either NotImplementedException or InvalidOperationException.
                }
            }

            static async Task ObserveAllExceptionsAsync()
            {
                var task1 = ThrowNotImplementedExceptionAsync();
                var task2 = ThrowInvalidOperationExceptionAsync();
                Task allTasks = Task.WhenAll(task1, task2);
                try
                {
                    await allTasks;
                }
                catch
                {
                    AggregateException allExceptions = allTasks.Exception;
                }
            }
        }

        public class Waiting_for_Any_Task_to_Complete
        {
            // Returns the length of data at the first URL to respond.
            private static async Task<int> FirstRespondingUrlAsync(string urlA, string urlB)
            {
                var httpClient = new HttpClient();
                // Start both downloads concurrently.
                Task<byte[]> downloadTaskA = httpClient.GetByteArrayAsync(urlA);
                Task<byte[]> downloadTaskB = httpClient.GetByteArrayAsync(urlB);
                // Wait for either of the tasks to complete.
                Task<byte[]> completedTask =
                await Task.WhenAny(downloadTaskA, downloadTaskB);
                // Return the length of the data retrieved from that URL.
                byte[] data = await completedTask;
                return data.Length;
            }
        }

        public class Processing_Tasks_as_They_Complete
        {
            static async Task<int> DelayAndReturnAsync(int val)
            {
                await Task.Delay(TimeSpan.FromSeconds(val));
                return val;
            }

            static async Task AwaitAndProcessAsync(Task<int> task)
            {
                var result = await task;
                Trace.WriteLine(result);
            }

            public static async Task ProcessTasksAsync()
            {
                // Create a sequence of tasks.
                Task<int> taskA = DelayAndReturnAsync(2);
                Task<int> taskB = DelayAndReturnAsync(3);
                Task<int> taskC = DelayAndReturnAsync(1);
                var tasks = new[] { taskA, taskB, taskC };
                var processingTasks = (from t in tasks select AwaitAndProcessAsync(t)).ToArray();
                // Await all processing to complete
                await Task.WhenAll(processingTasks);
            }
        }

        public class Avoiding_Context_for_Continuations
        {
            async Task ResumeOnContextAsync()
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                // This method resumes within the same context.
            }
            async Task ResumeWithoutContextAsync()
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                // This method discards its context when it resumes.
            }
        }

        public class Handling_Exceptions_from_async_Task_Methods
        {
            static async Task ThrowExceptionAsync()
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                throw new InvalidOperationException("Test");
            }
            static async Task TestAsync()
            {
                // The exception is thrown by the method and placed on the task.

                Task task = ThrowExceptionAsync();
                try
                {
                    // The exception is reraised here, where the task is awaited.
                    await task;
                }
                catch (InvalidOperationException)
                {
                    // The exception is correctly caught here.
                }
            }
        }

        public class Handling_Exceptions_from_async_Void_Methods
        {
            sealed class MyAsyncCommand : ICommand
            {
                public event EventHandler CanExecuteChanged;

                async void ICommand.Execute(object parameter)
                {
                    await Execute(parameter);
                }
                public async Task Execute(object parameter)
                {
                    // Asynchronous command implementation goes here.
                }

                public bool CanExecute(object parameter)
                {
                    throw new NotImplementedException();
                }
                // Other members (CanExecute, etc)
            }
        }

        public static class AnsycDemo
        {
            public static void Run()
            {
                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("Press ESC key to exit");
                    var task = PrintNumberAsync();
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        Console.WriteLine(task.Result);
                    }

                    Thread.Sleep(100);
                }
            }

            static async Task<int> PrintNumberAsync()
            {
                //await Task.Delay(TimeSpan.FromSeconds(1));
                return await Task.FromResult(new Random().Next());
            }
        }
    }
}