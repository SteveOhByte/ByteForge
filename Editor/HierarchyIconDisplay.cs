#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    [InitializeOnLoad]
    public static class HierarchyIconDisplay
    {
        private static bool hierarchyHasFocus = false;
        private static EditorWindow hierarchyEditorWindow;

        static HierarchyIconDisplay()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            EditorWindow hierarchyWindow = GetExistingWindow("UnityEditor.SceneHierarchyWindow");
            if (hierarchyWindow != null)
            {
                hierarchyHasFocus = EditorWindow.focusedWindow != null &&
                                    EditorWindow.focusedWindow == hierarchyEditorWindow;
            }
        }

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