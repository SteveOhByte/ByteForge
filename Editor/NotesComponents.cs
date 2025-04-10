#if UNITY_EDITOR
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Utility class providing helper methods for the notes system.
    /// </summary>
    /// <remarks>
    /// Contains methods for generating unique identifiers for components and
    /// resolving GameObject hierarchy paths. These methods support the developer
    /// notes feature that allows attaching comments to specific components.
    /// </remarks>
    public static class NotesUtility
    {
        /// <summary>
        /// Generates a unique key for identifying a specific component on a GameObject.
        /// </summary>
        /// <param name="targetComponent">The component to generate a key for.</param>
        /// <returns>A string that uniquely identifies the component within the scene.</returns>
        /// <remarks>
        /// The key combines the full hierarchy path of the GameObject with the component type,
        /// ensuring that each component can be uniquely identified even if multiple
        /// GameObjects have the same component types.
        /// </remarks>
        public static string GetNoteKey(MonoBehaviour targetComponent)
        {
            return $"{GetGameObjectPath(targetComponent.gameObject)}_{targetComponent.GetType()}";
        }

        /// <summary>
        /// Gets the full hierarchical path to a GameObject in the scene.
        /// </summary>
        /// <param name="obj">The GameObject to get the path for.</param>
        /// <returns>A string representing the full path from the root to the GameObject.</returns>
        /// <remarks>
        /// The path is constructed by walking up the transform hierarchy from the GameObject
        /// to the root, creating a string in the format "/RootObject/Parent/Child".
        /// This provides a unique way to identify GameObjects in the scene.
        /// </remarks>
        public static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }

            return path;
        }
    }
}
#endif