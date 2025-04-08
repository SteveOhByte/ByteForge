using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ByteForge.Runtime
{
    /// <summary>
    /// A generic singleton base class for MonoBehaviours
    /// Simply inherit from this instead of MonoBehaviour to make your class a singleton.
    /// </summary>
    /// <typeparam name="T">The type of the singleton</typeparam>
    public class BFSingleton<T> : MonoBehaviour where T : BFSingleton<T>
    {
        private static T instance;
        private static bool isQuitting = false;
        private static bool isInitialized = false;
        private static bool debugLogging = false;
        private static readonly object lockObject = new();

        public static T Instance
        {
            get
            {
                if (isQuitting)
                {
                    if (debugLogging) 
                        Debug.LogWarning($"[BFSingleton] Instance of {typeof(T).Name} was requested during application quit. Returning null.");
                    return null;
                }

                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = FindFirstObjectByType<T>();

                        if (instance == null)
                        {
                            try
                            {
                                GameObject singleton = new($"[Singleton] {typeof(T).Name}");
                                instance = singleton.AddComponent<T>();
                                DontDestroyOnLoad(singleton);

                                if (debugLogging)
                                    Debug.Log(
                                        $"[BFSingleton] An instance of {typeof(T).Name} was created with DontDestroyOnLoad.");
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"[BFSingleton] Failed to create instance of {typeof(T).Name}: {e.Message}");
                                return null;
                            }
                        }
                        else
                        {
                            DontDestroyOnLoad(instance.gameObject);
                            if (debugLogging)
                                Debug.Log($"[BFSingleton] Using existing instance of {typeof(T).Name} with DontDestroyOnLoad.");
                        }

                        if (!isInitialized)
                        {
                            instance.Initialize();
                            isInitialized = true;
                        }
                    }

                    return instance;
                }
            }
        }
    
        public static bool HasInstance => instance != null && !isQuitting;

        public static bool DebugLogging
        {
            get => debugLogging;
            set => debugLogging = value;
        }
    
        protected virtual void Initialize() { }

        protected virtual void Awake()
        {
            lock (lockObject)
            {
                if (instance == null)
                {
                    instance = this as T;
                    DontDestroyOnLoad(gameObject);

                    if (!isInitialized)
                    {
                        Initialize();
                        isInitialized = true;
                    }
                
                    if (debugLogging)
                        Debug.Log($"[BFSingleton] Initialized singleton instance of {typeof(T).Name}.");
                }
                else
                {
                    if (debugLogging)
                        Debug.LogWarning($"[BFSingleton] Multiple instances of {typeof(T).Name} detected. Destroying duplicate.");
                    Destroy(gameObject);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            lock (lockObject)
            {
                if (instance == this)
                {
                    if (debugLogging)
                        Debug.Log($"[BFSingleton] Instance of {typeof(T).Name} has been destroyed.");
                    instance = null;
                    isInitialized = false;
                }
            }
        }
    
        protected virtual void OnApplicationQuit()
        {
            lock (lockObject)
            {
                isQuitting = true;
                if (debugLogging)
                    Debug.Log($"[BFSingleton] Application is quitting. {typeof(T).Name} marked as quitting.");
            }
        }

        protected virtual void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    
        protected virtual void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            lock (lockObject)
                isQuitting = false;
        }

        /// <summary>
        /// Clears the singleton instance, forcing a new one to be created.
        /// Use with caution.
        /// </summary>
        public static void ClearInstance()
        {
            lock (lockObject)
            {
                if (instance != null)
                {
                    if (debugLogging)
                        Debug.Log($"[BFSingleton] Clearing instance of {typeof(T).Name}.");
                    Destroy(instance.gameObject);
                    instance = null;
                    isInitialized = false;
                }
            }
        }

        /// <summary>
        /// Force creation of the singleton instance if it doesn't exist.
        /// </summary>
        /// <returns>The singleton instance.</returns>
        public static T CreateInstanceIfNeeded()
        {
            return Instance;
        }

        /// <summary>
        /// Safely attempt to get the singleton instance without creating it.
        /// </summary>
        /// <param name="fetchedInstance">Out parameter for the instance.</param>
        /// <returns>True if the instance exists, false otherwise.</returns>
        public static bool TryGetInstance(out T fetchedInstance)
        {
            fetchedInstance = null;

            if (HasInstance)
            {
                fetchedInstance = instance;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Create a singleton from a prefab.
        /// </summary>
        /// <param name="prefab">The prefab containing the singleton component.</param>
        /// <returns>The singleton instance.</returns>
        public static T CreateFromPrefab(T prefab)
        {
            if (prefab == null)
            {
                Debug.LogError($"[BFSingleton] Cannot create singleton of type {typeof(T).Name} from null prefab.");
                return null;
            }

            lock (lockObject)
            {
                // If an instance already exists, return it
                if (instance != null)
                    return instance;
            
                T newInstance = Instantiate(prefab);
            
                // Name the object
                instance.gameObject.name = $"[Singleton] {typeof(T).Name} (from prefab)";
                
                if (debugLogging)
                    Debug.Log($"[BFSingleton] Created instance of {typeof(T).Name} from prefab.");
                
                // The Awake() method will handle setting up the instance
                return instance;
            }
        }
    }
}