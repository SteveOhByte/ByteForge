using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ByteForge.Runtime
{
    /// <summary>
    /// A handle to an async operation that can be used to stop it or await its completion.
    /// </summary>
    /// <remarks>
    /// AsyncHandle provides a way to interact with running async operations similar to how
    /// Coroutine handles work in Unity. The handle can be awaited directly, allowing
    /// async methods to wait for other async operations to complete. It can also be passed to
    /// HaltAsync to cancel the operation.
    /// </remarks>
    public readonly struct AsyncHandle
    {
        /// <summary>
        /// The unique identifier for this async operation.
        /// </summary>
        /// <remarks>
        /// This ID is used internally to track the operation and its associated cancellation token.
        /// </remarks>
        public readonly int Id;
        
        /// <summary>
        /// The underlying task representing this async operation.
        /// </summary>
        /// <remarks>
        /// This task is used to enable awaiting the AsyncHandle directly.
        /// </remarks>
        private readonly Task task;

        /// <summary>
        /// Initializes a new instance of the AsyncHandle struct.
        /// </summary>
        /// <param name="id">The unique identifier for the async operation.</param>
        /// <param name="task">The task representing the async operation.</param>
        /// <remarks>
        /// This constructor is typically called by the AsyncSystem when starting a new async operation.
        /// </remarks>
        public AsyncHandle(int id, Task task)
        {
            Id = id;
            this.task = task;
        }

        /// <summary>
        /// Gets an awaiter that allows awaiting this AsyncHandle, similar to how coroutines can yield return other coroutines.
        /// </summary>
        /// <returns>A TaskAwaiter that can be used to await the completion of this async operation.</returns>
        /// <remarks>
        /// This method enables using the 'await' keyword directly on an AsyncHandle,
        /// making it possible to chain async operations in a way similar to how coroutines
        /// can yield return other coroutines.
        /// 
        /// Example:
        /// <code>
        /// AsyncHandle handle = someObject.RunAsync(() => SomeAsyncMethod());
        /// await handle; // Waits for the async operation to complete
        /// </code>
        /// </remarks>
        public TaskAwaiter GetAwaiter()
        {
            return task.GetAwaiter();
        }
    }
    
    /// <summary>
    /// A handle to an async operation that returns a value. It can be used to stop the operation or await its completion and result.
    /// </summary>
    /// <typeparam name="T">The type of the value that will be returned by the async operation.</typeparam>
    /// <remarks>
    /// AsyncHandle&lt;T&gt; extends the functionality of AsyncHandle by allowing the async operation
    /// to return a value when it completes. This provides a capability that traditional coroutines
    /// don't have, making it possible to get results from async operations.
    /// </remarks>
    public readonly struct AsyncHandle<T>
    {
        /// <summary>
        /// The unique identifier for this async operation.
        /// </summary>
        /// <remarks>
        /// This ID is used internally to track the operation and its associated cancellation token.
        /// </remarks>
        public readonly int Id;
        
        /// <summary>
        /// The underlying task representing this async operation with a result.
        /// </summary>
        /// <remarks>
        /// This task is used to enable awaiting the AsyncHandle directly and retrieving its result.
        /// </remarks>
        private readonly Task<T> task;

        /// <summary>
        /// Initializes a new instance of the AsyncHandle&lt;T&gt; struct.
        /// </summary>
        /// <param name="id">The unique identifier for the async operation.</param>
        /// <param name="task">The task representing the async operation with a result.</param>
        /// <remarks>
        /// This constructor is typically called by the AsyncSystem when starting a new async operation that returns a value.
        /// </remarks>
        public AsyncHandle(int id, Task<T> task)
        {
            Id = id;
            this.task = task;
        }

        /// <summary>
        /// Gets an awaiter that allows awaiting this AsyncHandle and getting its result.
        /// </summary>
        /// <returns>A TaskAwaiter&lt;T&gt; that can be used to await the completion of this async operation and get its result.</returns>
        /// <remarks>
        /// This method enables using the 'await' keyword directly on an AsyncHandle&lt;T&gt;,
        /// making it possible to get the result of an async operation.
        /// 
        /// Example:
        /// <code>
        /// AsyncHandle&lt;int&gt; handle = someObject.RunAsync(() => SomeAsyncMethodReturningInt());
        /// int result = await handle; // Waits for the async operation to complete and gets its result
        /// </code>
        /// </remarks>
        public TaskAwaiter<T> GetAwaiter()
        {
            return task.GetAwaiter();
        }
    }
}