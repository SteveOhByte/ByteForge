#if UNITY_EDITOR
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Utility for notes-related operations.
    /// </summary>
    public static class NotesUtility
    {
        /// <summary>
        /// Gets the unique key for a component.
        /// </summary>
        public static string GetNoteKey(MonoBehaviour targetComponent)
        {
            return $"{GetGameObjectPath(targetComponent.gameObject)}_{targetComponent.GetType()}";
        }

        /// <summary>
        /// Gets the full path to a GameObject in the hierarchy.
        /// </summary>
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