#if UNITY_EDITOR
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Automatically displays component type icons next to GameObject names in the Unity Hierarchy window.
    /// </summary>
    /// <remarks>
    /// This utility enhances the hierarchy view by adding component icons to GameObjects,
    /// making it easier to identify objects by their primary component type at a glance.
    /// It automatically initializes when the editor loads and subscribes to hierarchy window events.
    /// </remarks>
    [InitializeOnLoad]
    [SuppressMessage("Domain reload", "UDR0001:Domain Reload Analyzer")]
    public static class HierarchyIconDisplay
    {
        /// <summary>
        /// Tracks whether the hierarchy window currently has focus.
        /// </summary>
        /// <remarks>
        /// This is used to adjust the appearance of selected items based on focus state.
        /// </remarks>
        private static bool hierarchyHasFocus = false;
        
        /// <summary>
        /// Reference to the hierarchy editor window.
        /// </summary>
        private static EditorWindow hierarchyEditorWindow;

        /// <summary>
        /// Static constructor that subscribes to editor events.
        /// </summary>
        /// <remarks>
        /// The [InitializeOnLoad] attribute ensures this constructor is called when
        /// the editor loads, setting up the hierarchy icon display functionality.
        /// </remarks>
        static HierarchyIconDisplay()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// Updates the focus state of the hierarchy window.
        /// </summary>
        /// <remarks>
        /// This method is called during the editor update cycle to track when
        /// the hierarchy window gains or loses focus, which affects the appearance
        /// of selected items.
        /// </remarks>
        private static void OnEditorUpdate()
        {
            EditorWindow hierarchyWindow = GetExistingWindow("UnityEditor.SceneHierarchyWindow");
            if (hierarchyWindow != null)
            {
                hierarchyHasFocus = EditorWindow.focusedWindow != null &&
                                    EditorWindow.focusedWindow == hierarchyEditorWindow;
            }
        }

        /// <summary>
        /// Gets a reference to an existing editor window by type name.
        /// </summary>
        /// <param name="typeName">The name of the window type to find.</param>
        /// <returns>The editor window if found; otherwise, null.</returns>
        /// <remarks>
        /// Uses reflection to find an existing window without creating a new one.
        /// This is used to locate the hierarchy window.
        /// </remarks>
        private static EditorWindow GetExistingWindow(string typeName)
        {
            Type type = Type.GetType(typeName + ",UnityEditor");
            if (type == null) return null;

            // Use reflection to avoid creating a new window.
            MethodInfo getAllWindowsMethod = typeof(EditorWindow).GetMethod("GetAll",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (getAllWindowsMethod == null) return null;

            if (getAllWindowsMethod.Invoke(null, null) is EditorWindow[] allWindows)
            {
                return allWindows.FirstOrDefault(window => window.GetType() == type);
            }

            return null;
        }

        /// <summary>
        /// Draws the component icon for a GameObject in the hierarchy window.
        /// </summary>
        /// <param name="instanceID">The instance ID of the item being drawn.</param>
        /// <param name="selectionRect">The rectangle area for the hierarchy item.</param>
        /// <remarks>
        /// This method is called for each item in the hierarchy window and draws
        /// the icon of the primary component (typically the first non-Transform component)
        /// to help identify the GameObject's purpose visually.
        /// </remarks>
        private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            try
            {
                Component[] components = obj.GetComponents<Component>();
                if (components == null || components.Length == 0) return;

                Component component = components.Length > 1 ? components[1] : components[0];
                if (component == null) return; // Added null check for safety

                Type type = component.GetType();

                GUIContent content = EditorGUIUtility.ObjectContent(component, type);
                content.text = null;
                content.tooltip = type.Name;

                // Fallback to default icon if content.image is null
                if (content.image == null)
                    content.image = EditorGUIUtility.IconContent("DefaultAsset Icon").image; // Use default icon

                bool isSelected = Selection.instanceIDs.Contains(instanceID);
                bool isHovered = selectionRect.Contains(Event.current.mousePosition);

                Color colour = UnityEditorBackgroundColour.Get(isSelected, isHovered, hierarchyHasFocus);
                Rect backgroundRect = selectionRect;
                backgroundRect.width = 18.2f;
                EditorGUI.DrawRect(backgroundRect, colour);

                EditorGUI.LabelField(selectionRect, content);
            }
            catch (Exception ex)
            {
                BFDebug.LogWarning($"Error displaying icon for GameObject {obj.name}: {ex.Message}");
                // If anything fails, use a fallback/default icon
                GUIContent defaultContent = EditorGUIUtility.IconContent("DefaultAsset Icon");
                EditorGUI.LabelField(selectionRect, defaultContent);
            }
        }
    }
}
#endif