#if UNITY_EDITOR // Ensure this code is only compiled and used in the editor, not in builds
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ByteForge.Editor
{
    /// <summary>
    /// Checks for missing required objects when entering play mode.
    /// </summary>
    [InitializeOnLoad]
    public class PlayModeStateChecker
    {
        static PlayModeStateChecker()
        {
            EditorApplication.playModeStateChanged +=
                OnPlayModeStateChanged; // Subscribe to the play mode state changed event.
        }

        /// <summary>
        /// Returns a list of all missing required objects in all open scenes.
        /// </summary>
        /// <returns>A list of all missing required objects in all open scenes.</returns>
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
        /// Returns all root game objects in all open scenes.
        /// </summary>
        /// <returns>All root game objects in all open scenes.</returns>
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