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
        public static void HaltAsync<T>(this MonoBehaviour monoBehaviour, AsyncHandle<T> handle)
        {
            AsyncSystem.Halt(new AsyncHandle(handle.Id, Task.CompletedTask));
        }
    }

    /// <summary>
    /// Utility class for executing expressions.
    /// </summary>
    internal static class ExpressionExecutor
    {
        /// <summary>
        /// Executes an async expression.
        /// </summary>
        public static Task ExecuteAsync(Expression<Func<Task>> expression)
        {
            Func<Task> func = expression.Compile();
            return func();
        }

        /// <summary>
        /// Executes an async expression with cancellation.
        /// </summary>
        public static Task ExecuteAsync(Expression<Func<CancellationToken, Task>> expression, CancellationToken cancellationToken)
        {
            Func<CancellationToken, Task> func = expression.Compile();
            return func(cancellationToken);
        }

        /// <summary>
        /// Executes an async expression that returns a value.
        /// </summary>
        public static Task<T> ExecuteAsync<T>(Expression<Func<Task<T>>> expression)
        {
            Func<Task<T>> func = expression.Compile();
            return func();
        }

        /// <summary>
        /// Executes an async expression with cancellation that returns a value.
        /// </summary>
        public static Task<T> ExecuteAsync<T>(Expression<Func<CancellationToken, Task<T>>> expression, CancellationToken cancellationToken)
        {
            Func<CancellationToken, Task<T>> func = expression.Compile();
            return func(cancellationToken);
        }
    }
}