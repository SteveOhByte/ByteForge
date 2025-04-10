#if UNITY_EDITOR // Ensure this code is only compiled and used in the editor, not in builds
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ByteForge.Editor
{
    /// <summary>
    /// Editor utility that checks for missing required components before entering play mode.
    /// </summary>
    /// <remarks>
    /// This class automatically initializes when the editor loads and subscribes to
    /// Unity's play mode state change events. When the user attempts to enter play mode,
    /// it checks for any object references marked with the [Required] attribute that
    /// haven't been assigned, preventing play mode and displaying an error message.
    /// </remarks>
    [InitializeOnLoad]
    [SuppressMessage("Domain reload", "UDR0001:Domain Reload Analyzer")]
    public class PlayModeStateChecker
    {
        /// <summary>
        /// Static constructor that subscribes to the play mode state changed event.
        /// </summary>
        /// <remarks>
        /// The [InitializeOnLoad] attribute ensures this constructor is called when
        /// the editor loads, setting up the play mode validation.
        /// </remarks>
        static PlayModeStateChecker()
        {
            EditorApplication.playModeStateChanged +=
                OnPlayModeStateChanged; // Subscribe to the play mode state changed event.
        }

        /// <summary>
        /// Finds all objects with missing required references in all open scenes.
        /// </summary>
        /// <returns>A list of strings describing each missing required reference.</returns>
        /// <remarks>
        /// Uses reflection to find all fields marked with the [Required] attribute,
        /// then checks if any of these fields have null references. For each missing
        /// reference, returns a descriptive string with the GameObject name, Component type,
        /// and field name.
        /// </remarks>
        private static List<string> GetMissingRequiredObjects()
        {
            return (from rootGameObject in GetAllRootGameObjectsInAllOpenScenes()
                    from component in rootGameObject
                        .GetComponentsInChildren<Component>(true)
                    let serializedObject = new SerializedObject(component)
                    from field in component
                        .GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(f => f.GetCustomAttributes(typeof(Required), true).Length > 0)
                    let property = serializedObject
                        .FindProperty(field.Name)
                    where property is { propertyType: SerializedPropertyType.ObjectReference } &&
                          property.objectReferenceValue == null
                    select
                        $"GameObject: '{ObjectNames.NicifyVariableName(rootGameObject.name)}' | Component: '{ObjectNames.NicifyVariableName(component.GetType().Name)}' | Field: '{ObjectNames.NicifyVariableName(field.Name)}'")
                .ToList();
        }

        /// <summary>
        /// Handles play mode state changes and validates required references.
        /// </summary>
        /// <param name="state">The current play mode state change.</param>
        /// <remarks>
        /// When exiting edit mode (about to enter play mode), checks for missing
        /// required references and prevents play mode if any are found, displaying
        /// an error dialog with details about the missing references.
        /// </remarks>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Detect if about to enter play mode.
            if (state != PlayModeStateChange.ExitingEditMode) return;

            List<string> missingObjects = GetMissingRequiredObjects();
            if (missingObjects.Count == 0) return;

            // Prevent play mode from starting.
            EditorApplication.isPlaying = false;

            // Construct the error message.
            string errorMessage =
                "Some required objects are missing! Please assign them before entering play mode.\n\n";
            errorMessage +=
                string.Join("\n", missingObjects.Take(10)); // Limit to the first 10 to prevent a super long message.
            if (missingObjects.Count > 10)
            {
                errorMessage += $"\n... and {missingObjects.Count - 10} more.";
            }

            // Display a warning.
            EditorUtility.DisplayDialog("Missing Objects", errorMessage, "OK");
        }

        /// <summary>
        /// Gets all root GameObjects from all currently open scenes.
        /// </summary>
        /// <returns>An enumerable of all root GameObjects in all open scenes.</returns>
        /// <remarks>
        /// Uses Unity's SceneManager to iterate through all open scenes and collect
        /// their root GameObjects. This is used as the starting point for searching
        /// all objects in the scene hierarchy.
        /// </remarks>
        private static IEnumerable<GameObject> GetAllRootGameObjectsInAllOpenScenes()
        {
            // Loop through all open scenes.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                // Loop through all root game objects in the scene.
                foreach (GameObject rootGameObject in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    yield return rootGameObject; // Return the root game object.
                }
            }
        }
    }
}
#endif