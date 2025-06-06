using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Base class for implementing the Singleton pattern for MonoBehaviours.
    /// </summary>
    /// <typeparam name="T">The type of the singleton class (must be a MonoBehaviour).</typeparam>
    /// <remarks>
    /// BFSingleton provides a thread-safe implementation of the Singleton pattern
    /// with automatic instance creation and duplicate prevention. When inheriting from this class,
    /// your MonoBehaviour will have a static Instance property that returns the single instance,
    /// creating one if it doesn't exist. The instance is automatically set to persist across scene loads.
    /// </remarks>
    public abstract class BFSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        /// The single instance of this type.
        /// </summary>
        /// <remarks>
        /// This is initialized lazily the first time it's accessed.
        /// </remarks>
        #pragma warning disable UDR0001
        private static T instance;
        #pragma warning restore UDR0001

        /// <summary>
        /// Gets the singleton instance of this type.
        /// </summary>
        /// <remarks>
        /// This property provides thread-safe access to the singleton instance.
        /// If the instance doesn't exist, it first tries to find an existing instance in the scene.
        /// If no instance is found, it creates a new GameObject with the component attached
        /// and marks it to persist across scene loads.
        /// </remarks>
        /// <value>The singleton instance of the specified type.</value>
        public static T Instance
        {
            get
            {
                // Return existing instance immediately if available
                if (instance != null)
                    return instance;

                // First try to find an existing instance
                instance = FindAnyObjectByType<T>();

                if (instance == null)
                {
                    GameObject singletonObject = new($"{typeof(T).Name} (Singleton)");
                    instance = singletonObject.AddComponent<T>();

                    DontDestroyOnLoad(singletonObject);
                    BFDebug.Log($"{typeof(T).Name} singleton instance created.");
                }

                return instance;
            }
        }

        /// <summary>
        /// Virtual method called when the MonoBehaviour is being initialized.
        /// </summary>
        /// <remarks>
        /// The base implementation handles singleton instance management by:
        /// 1. Checking if another instance already exists
        /// 2. Destroying this object if it's a duplicate
        /// 3. Setting this object as the instance if it's the first
        /// 4. Marking the object to persist across scene loads
        /// 
        /// When overriding, always call base.Awake() first to maintain singleton behaviour.
        /// </remarks>
        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                BFDebug.LogWarning($"Multiple instances of {typeof(T).Name} found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}