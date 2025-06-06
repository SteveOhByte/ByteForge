using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Static class that handles application initialization before any scene is loaded.
    /// </summary>
    /// <remarks>
    /// The Bootstrap class provides a central location for early initialization code
    /// that needs to run before any scene-specific code. This includes system-wide
    /// configurations, service registrations, and other preparation steps.
    /// The initialization is triggered automatically by Unity's runtime initialization system.
    /// </remarks>
    public static class Bootstrap
    {
        /// <summary>
        /// Initializes the ByteForge framework and any required subsystems.
        /// </summary>
        /// <remarks>
        /// Unity automatically calls this method during the SubsystemRegistration phase,
        /// which occurs before scene loading. This is the earliest point at which code can be
        /// executed in the Unity runtime lifecycle, making it ideal for framework initialization.
        /// 
        /// Add initialization code here that should run before any other code in the application.
        /// Examples include:
        /// - Configuring logging systems
        /// - Setting up dependency injection
        /// - Initializing analytics
        /// - Loading configuration data
        /// - Setting up cross-cutting concerns like exception handling
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            
        }
    }
}