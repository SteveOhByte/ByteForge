#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Utility class for displaying temporary notifications in the Scene View.
    /// </summary>
    /// <remarks>
    /// Provides a simple way to show feedback to the user directly in the
    /// Scene View without requiring a modal dialog or console message.
    /// These notifications appear as floating messages that automatically
    /// disappear after a short time.
    /// </remarks>
    public static class Notification
    {
        /// <summary>
        /// Displays a temporary notification message in the active Scene View.
        /// </summary>
        /// <param name="text">The message text to display.</param>
        /// <remarks>
        /// The notification appears as an overlay in the last active Scene View
        /// and automatically disappears after approximately 2 seconds.
        /// This is useful for providing non-intrusive feedback about operations
        /// performed in the editor.
        /// </remarks>
        public static void Display(string text)
        {
            GUIContent content = new(text);
            SceneView.lastActiveSceneView?.ShowNotification(content, 2);
        }
    }
}
#endif