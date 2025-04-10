using System;
using System.Collections.Generic;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Singleton manager that handles Update and FixedUpdate callbacks for async operations.
    /// </summary>
    /// <remarks>
    /// This internal manager provides a way for async operations to synchronize with Unity's update cycle
    /// by registering callbacks to be executed during Update or FixedUpdate. It uses a queue-based system
    /// to safely handle dynamic registration and execution without concurrent modification issues.
    /// </remarks>
    internal class AsyncUpdateManager : BFSingleton<AsyncUpdateManager>
    {
        /// <summary>
        /// Queue of actions to be executed during Update.
        /// </summary>
        /// <remarks>
        /// Using a queue ensures that actions are processed in the order they were registered
        /// and prevents concurrent modification exceptions during execution.
        /// </remarks>
        private Queue<Action> updateQueue = new();
        
        /// <summary>
        /// Queue of actions to be executed during FixedUpdate.
        /// </summary>
        /// <remarks>
        /// Using a queue ensures that actions are processed in the order they were registered
        /// and prevents concurrent modification exceptions during execution.
        /// </remarks>
        private Queue<Action> fixedUpdateQueue = new();
        
        /// <summary>
        /// List of actions waiting to be added to the update queue.
        /// </summary>
        /// <remarks>
        /// Actions are staged here before being transferred to the update queue
        /// at the beginning of the next Update cycle.
        /// </remarks>
        private List<Action> pendingUpdateActions = new();
        
        /// <summary>
        /// List of actions waiting to be added to the fixed update queue.
        /// </summary>
        /// <remarks>
        /// Actions are staged here before being transferred to the fixed update queue
        /// at the beginning of the next FixedUpdate cycle.
        /// </remarks>
        private List<Action> pendingFixedUpdateActions = new();

        /// <summary>
        /// Registers an action to be executed during the next Update cycle.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <remarks>
        /// The action is added to the pending list and will be transferred to the update queue
        /// at the beginning of the next Update cycle. This ensures thread safety when registering
        /// actions from multiple sources.
        /// </remarks>
        public void RegisterForUpdate(Action action)
        {
            pendingUpdateActions.Add(action);
        }

        /// <summary>
        /// Registers an action to be executed during the next FixedUpdate cycle.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <remarks>
        /// The action is added to the pending list and will be transferred to the fixed update queue
        /// at the beginning of the next FixedUpdate cycle. This ensures thread safety when registering
        /// actions from multiple sources.
        /// </remarks>
        public void RegisterForFixedUpdate(Action action)
        {
            pendingFixedUpdateActions.Add(action);
        }

        /// <summary>
        /// Unity callback that executes on every frame.
        /// </summary>
        /// <remarks>
        /// Processes all pending update actions by transferring them to the update queue,
        /// then executes all actions in the queue. This two-step process ensures that
        /// actions registered during execution won't be processed until the next frame.
        /// </remarks>
        private void Update()
        {
            // Add any pending actions to the queue
            foreach (Action action in pendingUpdateActions)
                updateQueue.Enqueue(action);
            pendingUpdateActions.Clear();

            // Process all actions in the queue
            while (updateQueue.Count > 0)
            {
                Action action = updateQueue.Dequeue();
                action?.Invoke();
            }
        }

        /// <summary>
        /// Unity callback that executes on a fixed time interval.
        /// </summary>
        /// <remarks>
        /// Processes all pending fixed update actions by transferring them to the fixed update queue,
        /// then executes all actions in the queue. This two-step process ensures that
        /// actions registered during execution won't be processed until the next fixed update.
        /// </remarks>
        private void FixedUpdate()
        {
            // Add any pending actions to the queue
            foreach (Action action in pendingFixedUpdateActions)
                fixedUpdateQueue.Enqueue(action);
            pendingFixedUpdateActions.Clear();

            // Process all actions in the queue
            while (fixedUpdateQueue.Count > 0)
            {
                Action action = fixedUpdateQueue.Dequeue();
                action?.Invoke();
            }
        }
    }
}