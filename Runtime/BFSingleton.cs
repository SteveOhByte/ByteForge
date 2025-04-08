using System;
using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Generic singleton class for MonoBehaviours
    /// </summary>
    /// <typeparam name="T">Type of the singleton</typeparam>
    public abstract class BFSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // Private instance field
        private static T instance;

        // Public instance property with thread safety
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    // First try to find an existing instance in the scene
                    instance = FindAnyObjectByType<T>();

                    // If no instance exists, create a new one
                    if (instance == null)
                    {
                        // Create a new GameObject
                        GameObject singletonObject = new GameObject();
                        instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).Name + " (Singleton)";

                        // Make sure it persists between scenes if needed
                        DontDestroyOnLoad(singletonObject);

                        BFDebug.Log($"{typeof(T).Name} singleton instance created.");
                    }
                }

                return instance;
            }
        }
        
        protected bool isQuitting;

        // Make sure we don't have duplicate singletons when a scene loads
        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                // Duplicate singleton found, destroy this one
                BFDebug.LogWarning($"Multiple instances of {typeof(T).Name} found. Destroying duplicate.");
                Destroy(gameObject);
            }
            else if (instance == null)
            {
                // This is the first instance - make it the singleton
                instance = this as T;

                // Make sure it persists between scenes if needed
                DontDestroyOnLoad(gameObject);
            }
        }

        // Clean up on destroy
        protected virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        protected void OnApplicationQuit()
        {
            isQuitting = true;
        }
    }
}