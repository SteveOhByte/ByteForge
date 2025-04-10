using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Extension methods for MonoBehaviour to make using async methods as easy as coroutines.
    /// </summary>
    /// <remarks>
    /// This class provides a set of extension methods that allow MonoBehaviour scripts to
    /// use async/await patterns with the same ease and simplicity as traditional Unity coroutines.
    /// The methods offer strong typing, IDE support, and cancellation capabilities that
    /// traditional coroutines lack.
    /// </remarks>
    public static class AsyncExtensions
    {
        #region Basic RunAsync methods

        /// <summary>
        /// Runs an async function with strongly typed parameters using a lambda expression.
        /// Direct replacement for StartCoroutine with full IDE support.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour instance.</param>
        /// <param name="methodExpression">Lambda expression pointing to the method to run.</param>
        /// <returns>A handle that can be used to stop the async function or await its completion.</returns>
        /// <remarks>
        /// Example usage: this.RunAsync(() => MyAsyncMethod())
        /// This provides superior type safety and refactoring support compared to string-based coroutines.
        /// </remarks>
        public static AsyncHandle RunAsync(this MonoBehaviour monoBehaviour, Expression<Func<Task>> methodExpression)
        {
            return AsyncSystem.Run(async (ct) => await ExpressionExecutor.ExecuteAsync(methodExpression));
        }

        /// <summary>
        /// Runs an async function with strongly typed parameters and cancellation support.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour instance.</param>
        /// <param name="methodExpression">Lambda expression pointing to the method to run.</param>
        /// <returns>A handle that can be used to stop the async function or await its completion.</returns>
        /// <remarks>
        /// Example usage: this.RunAsync((ct) => MyAsyncMethod(ct))
        /// This variant allows passing a CancellationToken to the async method for cooperative cancellation.
        /// </remarks>
        public static AsyncHandle RunAsync(this MonoBehaviour monoBehaviour, Expression<Func<CancellationToken, Task>> methodExpression)
        {
            return AsyncSystem.Run(async (ct) => await ExpressionExecutor.ExecuteAsync(methodExpression, ct));
        }

        /// <summary>
        /// Runs an async function with strongly typed parameters that returns a value.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour instance.</param>
        /// <param name="methodExpression">Lambda expression pointing to the method to run.</param>
        /// <returns>A handle that can be used to stop the async function or await its result.</returns>
        /// <remarks>
        /// Example usage: var handle = this.RunAsync(() => MyAsyncMethodWithResult())
        /// Later, you can await the result: var result = await handle;
        /// This provides a way to get return values from async operations, which is not possible with coroutines.
        /// </remarks>
        public static AsyncHandle<T> RunAsync<T>(this MonoBehaviour monoBehaviour, Expression<Func<Task<T>>> methodExpression)
        {
            return AsyncSystem.Run<T>(async (ct) => await ExpressionExecutor.ExecuteAsync(methodExpression));
        }

        /// <summary>
        /// Runs an async function with strongly typed parameters and cancellation support that returns a value.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour instance.</param>
        /// <param name="methodExpression">Lambda expression pointing to the method to run.</param>
        /// <returns>A handle that can be used to stop the async function or await its result.</returns>
        /// <remarks>
        /// Example usage: var handle = this.RunAsync((ct) => MyAsyncMethodWithResult(ct))
        /// This variant combines return value support with cancellation capabilities.
        /// </remarks>
        public static AsyncHandle<T> RunAsync<T>(this MonoBehaviour monoBehaviour, Expression<Func<CancellationToken, Task<T>>> methodExpression)
        {
            return AsyncSystem.Run<T>(async (ct) => await ExpressionExecutor.ExecuteAsync(methodExpression, ct));
        }

        #endregion

        /// <summary>
        /// Stops an async function that was started with RunAsync or StartAsync.
        /// Direct replacement for StopCoroutine.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour instance.</param>
        /// <param name="handle">The handle of the async function to stop.</param>
        /// <remarks>
        /// This method signals cancellation to the running async task, allowing it to
        /// gracefully terminate if it respects the cancellation token.
        /// </remarks>
        public static void HaltAsync(this MonoBehaviour monoBehaviour, AsyncHandle handle)
        {
            AsyncSystem.Halt(handle);
        }

        /// <summary>
        /// Stops an async function that was started with RunAsync or StartAsync and returned a value.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour instance.</param>
        /// <param name="handle">The handle of the async function to stop.</param>
        /// <remarks>
        /// This method signals cancellation to the running async task, allowing it to
        /// gracefully terminate if it respects the cancellation token. The task will not
        /// produce a result if cancelled.
        /// </remarks>
        public static void HaltAsync<T>(this MonoBehaviour monoBehaviour, AsyncHandle<T> handle)
        {
            AsyncSystem.Halt(new AsyncHandle(handle.Id, Task.CompletedTask));
        }

        /// <summary>
        /// Waits until the end of the current frame, similar to WaitForEndOfFrame in coroutines.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour instance.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the wait.</param>
        /// <returns>A task that completes at the end of the current frame.</returns>
        /// <remarks>
        /// Example usage: await this.YieldForEndOfFrame();
        /// This is useful for spreading work across multiple frames or waiting for rendering to complete.
        /// </remarks>
        public static async Task YieldForEndOfFrame(this MonoBehaviour monoBehaviour, CancellationToken cancellationToken = default)
        {
            await AsyncSystem.WaitForEndOfFrame(cancellationToken);
        }
    }

    /// <summary>
    /// Utility class for executing expressions containing async methods.
    /// </summary>
    /// <remarks>
    /// This internal class handles the compilation and execution of lambda expressions
    /// containing async methods. It's used by the AsyncExtensions class to provide
    /// type-safe method references through lambda expressions.
    /// </remarks>
    internal static class ExpressionExecutor
    {
        /// <summary>
        /// Executes an async expression.
        /// </summary>
        /// <param name="expression">The lambda expression to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Compiles the lambda expression and invokes the resulting delegate.
        /// </remarks>
        public static Task ExecuteAsync(Expression<Func<Task>> expression)
        {
            Func<Task> func = expression.Compile();
            return func();
        }

        /// <summary>
        /// Executes an async expression with cancellation.
        /// </summary>
        /// <param name="expression">The lambda expression to execute.</param>
        /// <param name="cancellationToken">The cancellation token to pass to the method.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Compiles the lambda expression and invokes the resulting delegate with the provided cancellation token.
        /// </remarks>
        public static Task ExecuteAsync(Expression<Func<CancellationToken, Task>> expression, CancellationToken cancellationToken)
        {
            Func<CancellationToken, Task> func = expression.Compile();
            return func(cancellationToken);
        }

        /// <summary>
        /// Executes an async expression that returns a value.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="expression">The lambda expression to execute.</param>
        /// <returns>A task representing the asynchronous operation with a result.</returns>
        /// <remarks>
        /// Compiles the lambda expression and invokes the resulting delegate.
        /// </remarks>
        public static Task<T> ExecuteAsync<T>(Expression<Func<Task<T>>> expression)
        {
            Func<Task<T>> func = expression.Compile();
            return func();
        }

        /// <summary>
        /// Executes an async expression with cancellation that returns a value.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="expression">The lambda expression to execute.</param>
        /// <param name="cancellationToken">The cancellation token to pass to the method.</param>
        /// <returns>A task representing the asynchronous operation with a result.</returns>
        /// <remarks>
        /// Compiles the lambda expression and invokes the resulting delegate with the provided cancellation token.
        /// </remarks>
        public static Task<T> ExecuteAsync<T>(Expression<Func<CancellationToken, Task<T>>> expression, CancellationToken cancellationToken)
        {
            Func<CancellationToken, Task<T>> func = expression.Compile();
            return func(cancellationToken);
        }
    }
}