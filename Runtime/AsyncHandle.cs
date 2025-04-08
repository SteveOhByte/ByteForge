using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ByteForge.Runtime
{
    /// <summary>
    /// A handle to an async operation that can be used to stop it or await its completion.
    /// </summary>
    public readonly struct AsyncHandle
    {
        public readonly int Id;
        private readonly Task task;

        public AsyncHandle(int id, Task task)
        {
            Id = id;
            this.task = task;
        }

        /// <summary>
        /// Gets an awaiter that allows awaiting this AsyncHandle, similar to how coroutines can yield return other coroutines.
        /// </summary>
        public TaskAwaiter GetAwaiter()
        {
            return task.GetAwaiter();
        }
    }
    
    /// <summary>
    /// A handle to an async operation that returns a value. It can be used to stop the operation or await its completion and result.
    /// </summary>
    public readonly struct AsyncHandle<T>
    {
        public readonly int Id;
        private readonly Task<T> task;

        public AsyncHandle(int id, Task<T> task)
        {
            Id = id;
            this.task = task;
        }

        /// <summary>
        /// Gets an awaiter that allows awaiting this AsyncHandle and getting its result.
        /// </summary>
        public TaskAwaiter<T> GetAwaiter()
        {
            return task.GetAwaiter();
        }
    }
}