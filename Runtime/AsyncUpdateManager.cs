using System;
using System.Collections.Generic;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Singleton manager that handles Update and FixedUpdate callbacks.
    /// </summary>
    internal class AsyncUpdateManager : BFSingleton<AsyncUpdateManager>
    {
        // Using queues to avoid concurrent modification exceptions
        private Queue<Action> updateQueue = new();
        private Queue<Action> fixedUpdateQueue = new();
        private List<Action> pendingUpdateActions = new();
        private List<Action> pendingFixedUpdateActions = new();

        public void RegisterForUpdate(Action action)
        {
            pendingUpdateActions.Add(action);
        }

        public void RegisterForFixedUpdate(Action action)
        {
            pendingFixedUpdateActions.Add(action);
        }

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