using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// System that brings the power of C# async/await to Unity with the convenience of coroutines.
    /// </summary>
    /// <remarks>
    /// AsyncSystem is the core engine that manages async operations in Unity. It provides
    /// functionality similar to Unity's coroutine system but with the full power of C# async/await.
    /// This includes support for cancellation, return values, and exception handling,
    /// along with utility methods that mimic common coroutine yield instructions.
    /// </remarks>
    public static class AsyncSystem
    {
        /// <summary>
        /// Dictionary mapping async operation IDs to their cancellation tokens.
        /// </summary>
        /// <remarks>
        /// Used to track all active async operations and allow them to be cancelled.
        /// </remarks>
        private static readonly Dictionary<int, CancellationTokenSource> taskCancellationTokens = new();
        
        /// <summary>
        /// Counter for generating unique IDs for async operations.
        /// </summary>
        /// <remarks>
        /// Each new async operation gets an ID value based on this counter.
        /// </remarks>
        private static int nextId = 1;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            nextId = 1;
        }

        /// <summary>
        /// Starts an async function and returns a handle that can be used to stop it later or await its completion.
        /// </summary>
        /// <param name="asyncFunc">The async function to execute, which accepts a cancellation token.</param>
        /// <returns>An AsyncHandle that can be used to control or await the operation.</returns>
        /// <remarks>
        /// This method creates a cancellation token for the async operation, executes the function,
        /// and returns a handle that can be used to track or control the operation.
        /// When the operation completes (successfully or due to cancellation), it automatically
        /// removes its entry from the tracking dictionary.
        /// </remarks>
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
        /// <typeparam name="T">The type of the value that will be returned by the async operation.</typeparam>
        /// <param name="asyncFunc">The async function to execute, which accepts a cancellation token and returns a value.</param>
        /// <returns>An AsyncHandle&lt;T&gt; that can be used to control or await the operation and get its result.</returns>
        /// <remarks>
        /// This method creates a cancellation token for the async operation, executes the function,
        /// and returns a handle that can be used to track or control the operation.
        /// When the operation completes (successfully or due to cancellation), it automatically
        /// removes its entry from the tracking dictionary.
        /// </remarks>
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
        /// Stops an async function that was started with Run.
        /// </summary>
        /// <param name="handle">The handle of the async function to stop.</param>
        /// <remarks>
        /// This method signals cancellation to the specified async operation, which will
        /// cause it to terminate gracefully if it respects the cancellation token.
        /// The entry for the operation is removed from the tracking dictionary immediately.
        /// </remarks>
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
        /// <param name="seconds">The number of seconds to wait.</param>
        /// <param name="cancellationToken">Optional token to cancel the wait operation.</param>
        /// <returns>A task that completes after the specified delay.</returns>
        /// <remarks>
        /// This method provides functionality similar to WaitForSeconds in coroutines.
        /// It uses Task.Delay internally, which provides a non-blocking wait.
        /// The wait can be cancelled by the cancellation token.
        /// </remarks>
        public static async Task WaitForSeconds(float seconds, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return;
            
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
        }

        /// <summary>
        /// Waits until the end of the current frame.
        /// </summary>
        /// <param name="cancellationToken">Optional token to cancel the wait operation.</param>
        /// <returns>A task that completes at the end of the current frame.</returns>
        /// <remarks>
        /// This method provides functionality similar to WaitForEndOfFrame in coroutines.
        /// It uses WaitForUpdate internally, as Unity's update cycle handles end-of-frame operations.
        /// </remarks>
        public static Task WaitForEndOfFrame(CancellationToken cancellationToken = default)
        {
            return WaitForUpdate(cancellationToken);
        }

        /// <summary>
        /// Waits until the next frame update.
        /// </summary>
        /// <param name="cancellationToken">Optional token to cancel the wait operation.</param>
        /// <returns>A task that completes during the next Update cycle.</returns>
        /// <remarks>
        /// This method provides functionality similar to yielding null in coroutines.
        /// It registers a callback with the AsyncUpdateManager to be executed during
        /// the next Update cycle.
        /// </remarks>
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
        /// <param name="cancellationToken">Optional token to cancel the wait operation.</param>
        /// <returns>A task that completes during the next FixedUpdate cycle.</returns>
        /// <remarks>
        /// This method provides functionality similar to WaitForFixedUpdate in coroutines.
        /// It registers a callback with the AsyncUpdateManager to be executed during
        /// the next FixedUpdate cycle.
        /// </remarks>
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
        /// <param name="predicate">A function that determines when to stop waiting.</param>
        /// <param name="cancellationToken">Optional token to cancel the wait operation.</param>
        /// <returns>A task that completes when the predicate returns true or cancellation is requested.</returns>
        /// <remarks>
        /// This method provides functionality similar to WaitUntil in coroutines.
        /// It checks the predicate on each frame update until it returns true or
        /// cancellation is requested.
        /// </remarks>
        public static async Task WaitUntil(Func<bool> predicate, CancellationToken cancellationToken = default)
        {
            while (!predicate() && !cancellationToken.IsCancellationRequested)
                await WaitForUpdate(cancellationToken);
        }

        /// <summary>
        /// Waits while the specified predicate returns true.
        /// </summary>
        /// <param name="predicate">A function that determines when to stop waiting.</param>
        /// <param name="cancellationToken">Optional token to cancel the wait operation.</param>
        /// <returns>A task that completes when the predicate returns false or cancellation is requested.</returns>
        /// <remarks>
        /// This method provides functionality similar to WaitWhile in coroutines.
        /// It checks the predicate on each frame update until it returns false or
        /// cancellation is requested.
        /// </remarks>
        public static async Task WaitWhile(Func<bool> predicate, CancellationToken cancellationToken = default)
        {
            while (predicate() && !cancellationToken.IsCancellationRequested)
                await WaitForUpdate(cancellationToken);
        }
    }
}