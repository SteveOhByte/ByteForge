using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Central application initialization system for ByteForge.
    /// Provides a guaranteed early entry point similar to Program.cs in standard C# applications.
    /// </summary>
    public static class ByteForgeApp
    {
        #region Initialization Structures

        /// <summary>
        /// Defines when an initializer should run in the Unity lifecycle.
        /// </summary>
        public enum InitializationTime
        {
            BEFORE_SPLASH_SCREEN = 0,    // Before Unity splash screen
            BEFORE_SCENE_LOAD = 100,     // After splash screen but before any scene loads
            AFTER_SCENE_LOAD = 200,      // After first scene is loaded
            AFTER_ASSEMBLIES_LOADED = 300 // After all assemblies are loaded
        }

        /// <summary>
        /// Priority level for initializers with the same InitializationTime.
        /// </summary>
        public enum InitializationPriority
        {
            CRITICAL = 0,      // Must run first (logging, crash handling)
            VERY_HIGH = 100,    // Security, configuration
            HIGH = 200,        // Services dependencies
            NORMAL = 300,      // Standard initialization
            LOW = 400,         // Optional systems
            VERY_LOW = 500      // Deferred initialization
        }

        /// <summary>
        /// Represents a single initialization step.
        /// </summary>
        private class InitializationStep
        {
            public string Name { get; }
            public Action Action { get; }
            public InitializationTime Time { get; }
            public InitializationPriority Priority { get; }
            public bool HasRun { get; set; }
            public bool IsRequired { get; }

            public InitializationStep(string name, Action action, InitializationTime time, 
                                     InitializationPriority priority, bool isRequired)
            {
                Name = name;
                Action = action;
                Time = time;
                Priority = priority;
                IsRequired = isRequired;
                HasRun = false;
            }
        }

        #endregion

        #region Static Fields

        // List of initialization steps
        private static List<InitializationStep> initSteps = new();
        
        // Track application state
        private static bool isInitialized = false;
        private static bool isInitializing = false;
        private static bool hasCriticalError = false;
        private static Exception criticalException = null;
        
        // Configuration
        private static bool continueOnError = false;
        private static bool isDebugLoggingEnabled = true;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the application has completed initialization.
        /// </summary>
        public static bool IsInitialized => isInitialized;

        /// <summary>
        /// Gets whether a critical error occurred during initialization.
        /// </summary>
        public static bool HasCriticalError => hasCriticalError;

        /// <summary>
        /// Gets the critical exception if one occurred during initialization.
        /// </summary>
        public static Exception CriticalException => criticalException;

        /// <summary>
        /// Gets or sets whether to continue initialization if a non-required step fails.
        /// </summary>
        public static bool ContinueOnError
        {
            get => continueOnError;
            set => continueOnError = value;
        }

        /// <summary>
        /// Gets or sets whether debug logging is enabled.
        /// </summary>
        public static bool IsDebugLoggingEnabled
        {
            get => isDebugLoggingEnabled;
            set => isDebugLoggingEnabled = value;
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when initialization begins.
        /// </summary>
        public static event Action OnInitializationStarted;

        /// <summary>
        /// Event raised when initialization completes successfully.
        /// </summary>
        public static event Action OnInitializationCompleted;

        /// <summary>
        /// Event raised when initialization fails.
        /// </summary>
        public static event Action<Exception> OnInitializationFailed;

        /// <summary>
        /// Event raised when the application is about to quit.
        /// </summary>
        public static event Action OnApplicationQuitting;

        #endregion

        #region RuntimeInitializeOnLoad Methods

        /// <summary>
        /// Entry point that runs before the splash screen.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void InitializeBeforeSplashScreen()
        {
            if (isDebugLoggingEnabled)
                BFDebug.Log("[ByteForgeApp] Initializing before splash screen...");
                
            RunInitializationSteps(InitializationTime.BEFORE_SPLASH_SCREEN);
        }

        /// <summary>
        /// Entry point that runs before any scene is loaded.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            if (isDebugLoggingEnabled)
                BFDebug.Log("[ByteForgeApp] Initializing before scene load...");
                
            if (!isInitializing)
            {
                isInitializing = true;
                OnInitializationStarted?.Invoke();
            }
            
            RunInitializationSteps(InitializationTime.BEFORE_SCENE_LOAD);
        }

        /// <summary>
        /// Entry point that runs after the first scene is loaded.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAfterSceneLoad()
        {
            if (isDebugLoggingEnabled)
                BFDebug.Log("[ByteForgeApp] Initializing after scene load...");
                
            RunInitializationSteps(InitializationTime.AFTER_SCENE_LOAD);
        }

        /// <summary>
        /// Entry point that runs after all assemblies are loaded.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void InitializeAfterAssembliesLoaded()
        {
            if (isDebugLoggingEnabled)
                BFDebug.Log("[ByteForgeApp] Initializing after assemblies loaded...");
                
            RunInitializationSteps(InitializationTime.AFTER_ASSEMBLIES_LOADED);
            
            // Initialization is now complete
            if (!hasCriticalError)
            {
                isInitialized = true;
                if (isDebugLoggingEnabled)
                    BFDebug.Log("[ByteForgeApp] Initialization completed successfully");
                    
                OnInitializationCompleted?.Invoke();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Registers an initialization step to be executed at the specified time and priority.
        /// </summary>
        /// <param name="name">Name of the initialization step</param>
        /// <param name="action">Action to execute</param>
        /// <param name="time">When to execute the action</param>
        /// <param name="priority">Priority within the initialization time</param>
        /// <param name="isRequired">Whether the application should fail if this step fails</param>
        public static void RegisterInitializer(string name, Action action, 
                                              InitializationTime time = InitializationTime.BEFORE_SCENE_LOAD,
                                              InitializationPriority priority = InitializationPriority.NORMAL,
                                              bool isRequired = false)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Initializer name cannot be null or empty");
                
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            // Check for duplicate names
            if (initSteps.Any(step => step.Name == name))
            {
                BFDebug.LogWarning($"[ByteForgeApp] Initializer with name '{name}' already registered");
                return;
            }
            
            InitializationStep step = new(name, action, time, priority, isRequired);
            initSteps.Add(step);
            
            if (isDebugLoggingEnabled)
                BFDebug.Log($"[ByteForgeApp] Registered initializer: {name} (Time: {time}, Priority: {priority}, Required: {isRequired})");
                
            // If this time has already passed, run immediately
            if (isInitializing && HasTimePhaseStarted(time))
            {
                if (isDebugLoggingEnabled)
                    BFDebug.Log($"[ByteForgeApp] Running late initializer immediately: {name}");
                    
                RunInitializationStep(step);
            }
        }

        /// <summary>
        /// Registers a critical initializer that runs very early and must succeed.
        /// </summary>
        public static void RegisterCriticalInitializer(string name, Action action)
        {
            RegisterInitializer(name, action, InitializationTime.BEFORE_SPLASH_SCREEN, 
                               InitializationPriority.CRITICAL, true);
        }

        /// <summary>
        /// Executes an action only if the application has fully initialized.
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="failureAction">Optional action to execute if initialization failed</param>
        public static void RunWhenInitialized(Action action, Action failureAction = null)
        {
            if (action == null)
                return;
                
            if (isInitialized)
            {
                action();
            }
            else if (hasCriticalError && failureAction != null)
            {
                failureAction();
            }
            else
            {
                OnInitializationCompleted += action;
                
                if (failureAction != null)
                    OnInitializationFailed += (ex) => failureAction();
            }
        }

        /// <summary>
        /// Checks if a specific initialization step has run.
        /// </summary>
        public static bool HasInitializerRun(string name)
        {
            return initSteps.Any(step => step.Name == name && step.HasRun);
        }

        /// <summary>
        /// Gets names of all registered initializers.
        /// </summary>
        public static string[] GetRegisteredInitializers()
        {
            return initSteps.Select(step => step.Name).ToArray();
        }

        /// <summary>
        /// Registers a method to be called when the application quits.
        /// </summary>
        public static void RegisterShutdownHandler(Action handler)
        {
            if (handler != null)
                OnApplicationQuitting += handler;
        }

        /// <summary>
        /// Manually trigger initialization (useful for testing).
        /// </summary>
        public static void ForceInitialize()
        {
            if (isInitialized)
                return;
                
            if (isDebugLoggingEnabled)
                BFDebug.Log("[ByteForgeApp] Forcing initialization...");
                
            isInitializing = true;
            OnInitializationStarted?.Invoke();
            
            // Run all phases in order
            RunInitializationSteps(InitializationTime.BEFORE_SPLASH_SCREEN);
            RunInitializationSteps(InitializationTime.BEFORE_SCENE_LOAD);
            RunInitializationSteps(InitializationTime.AFTER_SCENE_LOAD);
            RunInitializationSteps(InitializationTime.AFTER_ASSEMBLIES_LOADED);
            
            if (!hasCriticalError)
            {
                isInitialized = true;
                if (isDebugLoggingEnabled)
                    BFDebug.Log("[ByteForgeApp] Forced initialization completed successfully");
                    
                OnInitializationCompleted?.Invoke();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Runs all initialization steps for the specified time.
        /// </summary>
        private static void RunInitializationSteps(InitializationTime time)
        {
            if (hasCriticalError && !continueOnError)
                return;
                
            // Get all steps for this initialization time, ordered by priority
            var steps = initSteps
                .Where(s => s.Time == time && !s.HasRun)
                .OrderBy(s => (int)s.Priority)
                .ToList();
                
            // Run each step
            foreach (var step in steps)
            {
                RunInitializationStep(step);
                
                // Stop if a critical error occurred and we're not continuing on error
                if (hasCriticalError && !continueOnError)
                    break;
            }
        }

        /// <summary>
        /// Runs a single initialization step.
        /// </summary>
        private static void RunInitializationStep(InitializationStep step)
        {
            if (step.HasRun)
                return;
                
            if (isDebugLoggingEnabled)
                BFDebug.Log($"[ByteForgeApp] Running initializer: {step.Name}");
                
            try
            {
                step.Action();
                step.HasRun = true;
                
                if (isDebugLoggingEnabled)
                    BFDebug.Log($"[ByteForgeApp] Initializer completed: {step.Name}");
            }
            catch (Exception ex)
            {
                BFDebug.LogError($"[ByteForgeApp] Initializer failed: {step.Name}\nError: {ex.Message}\n{ex.StackTrace}");
                
                if (step.IsRequired)
                {
                    hasCriticalError = true;
                    criticalException = ex;
                    OnInitializationFailed?.Invoke(ex);
                    
                    BFDebug.LogError($"[ByteForgeApp] Critical initialization error in required step: {step.Name}");
                }
                else if (!continueOnError)
                {
                    hasCriticalError = true;
                    criticalException = ex;
                    OnInitializationFailed?.Invoke(ex);
                }
            }
        }

        /// <summary>
        /// Checks if an initialization time phase has already started.
        /// </summary>
        private static bool HasTimePhaseStarted(InitializationTime time)
        {
            switch (time)
            {
                case InitializationTime.BEFORE_SPLASH_SCREEN:
                    return true; // Always started
                case InitializationTime.BEFORE_SCENE_LOAD:
                    return initSteps.Any(s => s.Time == InitializationTime.BEFORE_SPLASH_SCREEN && s.HasRun);
                case InitializationTime.AFTER_SCENE_LOAD:
                    return initSteps.Any(s => s.Time == InitializationTime.BEFORE_SCENE_LOAD && s.HasRun);
                case InitializationTime.AFTER_ASSEMBLIES_LOADED:
                    return initSteps.Any(s => s.Time == InitializationTime.AFTER_SCENE_LOAD && s.HasRun);
                default:
                    return false;
            }
        }

        #endregion

        #region Application Lifecycle Handling

        // Make sure we can detect application quitting
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterQuitHandler()
        {
            Application.quitting -= HandleApplicationQuit;
            Application.quitting += HandleApplicationQuit;
        }

        private static void HandleApplicationQuit()
        {
            if (isDebugLoggingEnabled)
                BFDebug.Log("[ByteForgeApp] Application is quitting");
                
            OnApplicationQuitting?.Invoke();
        }

        #endregion
    }

    /// <summary>
    /// A helper component that can be used to register initializers from a scene.
    /// </summary>
    public class ByteForgeAppInitializer : MonoBehaviour
    {
        [Serializable]
        public enum InitTime
        {
            BEFORE_SPLASH_SCREEN = 0,
            BEFORE_SCENE_LOAD = 1,
            AFTER_SCENE_LOAD = 2,
            AFTER_ASSEMBLIES_LOADED = 3
        }

        [Serializable]
        public enum InitPriority
        {
            CRITICAL = 0,
            VERY_HIGH = 1,
            HIGH = 2,
            NORMAL = 3,
            LOW = 4,
            VERY_LOW = 5
        }

        [FormerlySerializedAs("_initializerName")] [SerializeField] private string initializerName = "SceneInitializer";
        [FormerlySerializedAs("_initTime")] [SerializeField] private InitTime initTime = InitTime.BEFORE_SCENE_LOAD;
        [FormerlySerializedAs("_priority")] [SerializeField] private InitPriority priority = InitPriority.NORMAL;
        [FormerlySerializedAs("_isRequired")] [SerializeField] private bool isRequired = false;
        [FormerlySerializedAs("_runInAwake")] [SerializeField] private bool runInAwake = true;
        [FormerlySerializedAs("_runInStart")] [SerializeField] private bool runInStart = false;
        [FormerlySerializedAs("_destroyAfterRun")] [SerializeField] private bool destroyAfterRun = true;
        [FormerlySerializedAs("_onInitialize")] [SerializeField] private UnityEngine.Events.UnityEvent onInitialize;

        private ByteForgeApp.InitializationTime GetActualInitTime()
        {
            switch (initTime)
            {
                case InitTime.BEFORE_SPLASH_SCREEN:
                    return ByteForgeApp.InitializationTime.BEFORE_SPLASH_SCREEN;
                case InitTime.BEFORE_SCENE_LOAD:
                    return ByteForgeApp.InitializationTime.BEFORE_SCENE_LOAD;
                case InitTime.AFTER_SCENE_LOAD:
                    return ByteForgeApp.InitializationTime.AFTER_SCENE_LOAD;
                case InitTime.AFTER_ASSEMBLIES_LOADED:
                    return ByteForgeApp.InitializationTime.AFTER_ASSEMBLIES_LOADED;
                default:
                    return ByteForgeApp.InitializationTime.BEFORE_SCENE_LOAD;
            }
        }

        private ByteForgeApp.InitializationPriority GetActualPriority()
        {
            switch (priority)
            {
                case InitPriority.CRITICAL:
                    return ByteForgeApp.InitializationPriority.CRITICAL;
                case InitPriority.VERY_HIGH:
                    return ByteForgeApp.InitializationPriority.VERY_HIGH;
                case InitPriority.HIGH:
                    return ByteForgeApp.InitializationPriority.HIGH;
                case InitPriority.NORMAL:
                    return ByteForgeApp.InitializationPriority.NORMAL;
                case InitPriority.LOW:
                    return ByteForgeApp.InitializationPriority.LOW;
                case InitPriority.VERY_LOW:
                    return ByteForgeApp.InitializationPriority.VERY_LOW;
                default:
                    return ByteForgeApp.InitializationPriority.NORMAL;
            }
        }

        private void Awake()
        {
            if (runInAwake)
                RegisterAndRunInitializer();
        }

        private void Start()
        {
            if (runInStart)
                RegisterAndRunInitializer();
        }

        private void RegisterAndRunInitializer()
        {
            string name = string.IsNullOrEmpty(initializerName) ? 
                $"SceneInitializer_{gameObject.name}" : initializerName;
                
            ByteForgeApp.RegisterInitializer(
                name,
                () => onInitialize?.Invoke(),
                GetActualInitTime(),
                GetActualPriority(),
                isRequired
            );
            
            if (destroyAfterRun)
                Destroy(this);
        }

        /// <summary>
        /// Can be called manually to run the initializer.
        /// </summary>
        public void RunInitializer()
        {
            RegisterAndRunInitializer();
        }
    }
}