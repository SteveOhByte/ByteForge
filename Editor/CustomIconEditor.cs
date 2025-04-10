#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ByteFroge.Editor
{
    /// <summary>
    /// Editor window for assigning custom icons to assets in the Project window.
    /// </summary>
    /// <remarks>
    /// This window allows developers to customize the icons displayed for various asset types
    /// in the Unity Project view. It provides a UI for selecting from built-in icons or
    /// assigning custom textures as icons for supported asset types.
    /// </remarks>
    [SuppressMessage("Domain reload", "UDR0005:Domain Reload Analyzer")]
    public class CustomIconEditor : EditorWindow
    {
        /// <summary>
        /// Prefix used for storing custom icon preferences in EditorPrefs.
        /// </summary>
        private const string ICON_PREFS_PREFIX = "CustomIcon_";
        
        /// <summary>
        /// The currently selected object to customize.
        /// </summary>
        private Object selectedObject;
        
        /// <summary>
        /// The custom icon texture assigned by the user.
        /// </summary>
        private Texture2D customIcon;
        
        /// <summary>
        /// Scroll position for the built-in icons list.
        /// </summary>
        private Vector2 scrollPosition;

        /// <summary>
        /// Set of asset types that support custom icons.
        /// </summary>
        /// <remarks>
        /// Only these asset types will allow icon customization through this tool.
        /// </remarks>
        private static readonly HashSet<string> supportedTypes = new()
        {
            "ScriptableObject",
            "TextAsset",
            "GameObject"
        };

        /// <summary>
        /// Shows the custom icon editor window.
        /// </summary>
        /// <remarks>
        /// This method is called when the "Assets/Set Custom Icon" menu item is selected.
        /// It creates and displays the custom icon editor window.
        /// </remarks>
        [MenuItem("Assets/Set Custom Icon")]
        public static void ShowWindow()
        {
            GetWindow<CustomIconEditor>("Custom Icons");
        }

        /// <summary>
        /// Called when the window becomes enabled.
        /// </summary>
        /// <remarks>
        /// Subscribes to the selection changed event to update the window
        /// when the user selects different assets.
        /// </remarks>
        private void OnEnable()
        {
            Selection.selectionChanged += Repaint;
        }

        /// <summary>
        /// Called when the window becomes disabled.
        /// </summary>
        /// <remarks>
        /// Unsubscribes from the selection changed event to prevent memory leaks.
        /// </remarks>
        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
        }

        /// <summary>
        /// Draws the GUI for the custom icon editor window.
        /// </summary>
        /// <remarks>
        /// Displays the current selection, custom icon selector, and built-in icons grid.
        /// Shows appropriate messages if no asset is selected or if the selected asset
        /// doesn't support custom icons.
        /// </remarks>
        private void OnGUI()
        {
            selectedObject = Selection.activeObject;
            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox("Select an object to customize its icon", MessageType.Info);
                return;
            }

            if (!IsSupported(selectedObject))
            {
                EditorGUILayout.HelpBox("Selected object type does not support custom icons", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(10);
            DrawIconSelector();
            EditorGUILayout.Space(10);
            DrawBuiltInIcons();
        }

        /// <summary>
        /// Determines if the selected object type supports custom icons.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object type supports custom icons; otherwise, false.</returns>
        /// <remarks>
        /// Supports prefabs, ScriptableObjects, and asset types listed in the supportedTypes collection.
        /// </remarks>
        private bool IsSupported(Object obj)
        {
            if (obj == null) return false;

            // Check if it's a prefab
            if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
                return true;

            if (obj is ScriptableObject)
                return true;

            return supportedTypes.Any(t => obj.GetType().Name.Contains(t));
        }

        /// <summary>
        /// Draws the custom icon selection interface.
        /// </summary>
        /// <remarks>
        /// Displays the current icon, a field for selecting a custom texture,
        /// and buttons for applying or resetting the icon.
        /// </remarks>
        private void DrawIconSelector()
        {
            EditorGUILayout.BeginHorizontal();

            // Display current icon
            Texture currentIcon = GetCustomIcon(selectedObject);
            EditorGUILayout.LabelField(new GUIContent(currentIcon, "Current Icon"), GUILayout.Width(64),
                GUILayout.Height(64));

            EditorGUILayout.BeginVertical();
            // Custom icon field
            customIcon = (Texture2D)EditorGUILayout.ObjectField("Custom Icon", customIcon, typeof(Texture2D), false);

            if (GUILayout.Button("Apply Custom Icon"))
            {
                if (customIcon != null)
                    SetCustomIcon(selectedObject, customIcon);
            }

            if (GUILayout.Button("Reset to Default"))
                ResetIcon(selectedObject);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the grid of built-in icons that can be applied.
        /// </summary>
        /// <remarks>
        /// Displays a scrollable grid of texture icons found in the project
        /// that can be applied to the selected asset with a single click.
        /// </remarks>
        private void DrawBuiltInIcons()
        {
            EditorGUILayout.LabelField("Built-in Icons", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            List<Texture> builtInIcons = GetBuiltInIcons();
            int columns = Mathf.FloorToInt(position.width / 70);
            int col = 0;

            EditorGUILayout.BeginHorizontal();
            foreach (Texture icon in builtInIcons)
            {
                if (col >= columns)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    col = 0;
                }

                if (GUILayout.Button(icon, GUILayout.Width(64), GUILayout.Height(64)))
                    SetCustomIcon(selectedObject, icon);
                col++;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Gets the current custom icon for an object.
        /// </summary>
        /// <param name="obj">The object to get the custom icon for.</param>
        /// <returns>The custom icon texture if one is set; otherwise, the default icon.</returns>
        /// <remarks>
        /// Retrieves the custom icon path from EditorPrefs and loads the texture.
        /// If no custom icon is set or the texture can't be loaded, returns the default icon.
        /// </remarks>
        private Texture GetCustomIcon(Object obj)
        {
            string iconKey = ICON_PREFS_PREFIX + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            string iconPath = EditorPrefs.GetString(iconKey, "");

            if (string.IsNullOrEmpty(iconPath))
                return EditorGUIUtility.FindTexture("DefaultAsset Icon");

            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        }

        /// <summary>
        /// Sets a custom icon for an object.
        /// </summary>
        /// <param name="obj">The object to set the custom icon for.</param>
        /// <param name="icon">The texture to use as the custom icon.</param>
        /// <remarks>
        /// Stores the icon path in EditorPrefs and marks the object as dirty
        /// to force Unity to refresh the icon in the Project view.
        /// </remarks>
        private void SetCustomIcon(Object obj, Texture icon)
        {
            string iconKey = ICON_PREFS_PREFIX + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            string iconPath = AssetDatabase.GetAssetPath(icon);
            EditorPrefs.SetString(iconKey, iconPath);

            // Force Unity to refresh the icon
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Resets an object's icon to the default.
        /// </summary>
        /// <param name="obj">The object to reset the icon for.</param>
        /// <remarks>
        /// Removes the custom icon entry from EditorPrefs and marks the object as dirty
        /// to force Unity to refresh the icon in the Project view.
        /// </remarks>
        private void ResetIcon(Object obj)
        {
            string iconKey = ICON_PREFS_PREFIX + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            EditorPrefs.DeleteKey(iconKey);
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Gets a list of built-in icons available in the project.
        /// </summary>
        /// <returns>A list of texture assets that can be used as icons.</returns>
        /// <remarks>
        /// Searches the project for texture assets with "icon" in their name
        /// to provide a collection of ready-to-use icons.
        /// </remarks>
        private List<Texture> GetBuiltInIcons()
        {
            List<Texture> icons = new();
            string[] guids = AssetDatabase.FindAssets("t:texture", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("icon") || path.Contains("Icon"))
                {
                    Texture icon = AssetDatabase.LoadAssetAtPath<Texture>(path);
                    if (icon != null)
                        icons.Add(icon);
                }
            }

            return icons;
        }
    }
}
#endif