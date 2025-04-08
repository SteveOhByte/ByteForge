using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ByteForge.Runtime
{
    /// <summary>
    /// System that brings the power of C# async/await to Unity with the convenience of coroutines.
    /// </summary>
    public static class AsyncSystem
    {
        private static readonly Dictionary<int, CancellationTokenSource> taskCancellationTokens = new();
        private static int nextId = 1;

        /// <summary>
        /// Starts an async function and returns a handle that can be used to stop it later or await its completion.
        /// </summary>
        public static AsyncHandle Run(Func<CancellationToken, Task> asyncFunc)
        {
            int id = nextId++;
            CancellationTokenSource cts = new();
            taskCancellationTokens[id] = cts;

            Task task = asyncFunc(cts.Token);
            
            // When the task completes, remove the cancellation token
            task.ContinueWith(t => 
            {
                taskCancellationTokens.Remove(id);
            });

            return new AsyncHandle(id, task);
        }
        
        /// <summary>
        /// Starts an async function with a return value and returns a handle that can be used to stop it or await its completion and result.
        /// </summary>
        public static AsyncHandle<T> Run<T>(Func<CancellationToken, Task<T>> asyncFunc)
        {
            int id = nextId++;
            CancellationTokenSource cts = new();
            taskCancellationTokens[id] = cts;

            Task<T> task = asyncFunc(cts.Token);
            
            // When the task completes, remove the cancellation token
            task.ContinueWith(t => 
            {
                taskCancellationTokens.Remove(id);
            });

            return new AsyncHandle<T>(id, task);
        }

        /// <summary>
        /// Stops an async function that was started with Start.
        /// </summary>
        public static void Halt(AsyncHandle handle)
        {
            if (taskCancellationTokens.TryGetValue(handle.Id, out CancellationTokenSource cts))
            {
                cts.Cancel();
                taskCancellationTokens.Remove(handle.Id);
            }
        }

        /// <summary>
        /// Waits for the specified number of seconds.
        /// </summary>
        public static async Task WaitForSeconds(float seconds, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return;
            
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
        }

        /// <summary>
        /// Waits until the next frame.
        /// </summary>
        public static Task WaitForEndOfFrame(CancellationToken cancellationToken = default)
        {
            return WaitForUpdate(cancellationToken);
        }

        /// <summary>
        /// Waits until the next frame.
        /// </summary>
        public static Task WaitForUpdate(CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<bool> tcs = new();
            
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.SetCanceled();
                return tcs.Task;
            }

            // Register for cancellation
            if (cancellationToken != CancellationToken.None)
                cancellationToken.Register(() => tcs.TrySetCanceled());

            AsyncUpdateManager.Instance.RegisterForUpdate(() => tcs.TrySetResult(true));
            return tcs.Task;
        }

        /// <summary>
        /// Waits until the next fixed update.
        /// </summary>
        public static Task WaitForFixedUpdate(CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<bool> tcs = new();
            
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.SetCanceled();
                return tcs.Task;
            }

            // Register for cancellation
            if (cancellationToken != CancellationToken.None)
                cancellationToken.Register(() => tcs.TrySetCanceled());

            AsyncUpdateManager.Instance.RegisterForFixedUpdate(() => tcs.TrySetResult(true));
            return tcs.Task;
        }

        /// <summary>
        /// Waits until the specified predicate returns true.
        /// </summary>
        public static async Task WaitUntil(Func<bool> predicate, CancellationToken cancellationToken = default)
        {
            while (!predicate() && !cancellationToken.IsCancellationRequested)
                await WaitForUpdate(cancellationToken);
        }

        /// <summary>
        /// Waits while the specified predicate returns true.
        /// </summary>
        public static async Task WaitWhile(Func<bool> predicate, CancellationToken cancellationToken = default)
        {
            while (predicate() && !cancellationToken.IsCancellationRequested)
                await WaitForUpdate(cancellationToken);
        }
    }
}