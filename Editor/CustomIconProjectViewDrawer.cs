#if UNITY_EDITOR
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Handles drawing custom icons in the Unity Project view.
    /// </summary>
    /// <remarks>
    /// This utility automatically initializes when the editor loads and subscribes to
    /// the project window item GUI event to draw custom icons for assets that have them.
    /// It works in conjunction with the CustomIconEditor to display the icons selected by users.
    /// </remarks>
    [InitializeOnLoad]
    [SuppressMessage("Domain reload", "UDR0001:Domain Reload Analyzer")]
    public class CustomIconProjectViewDrawer
    {
        /// <summary>
        /// Static constructor that subscribes to the project window item GUI event.
        /// </summary>
        /// <remarks>
        /// The [InitializeOnLoad] attribute ensures this constructor is called when
        /// the editor loads, setting up the custom icon drawing functionality.
        /// </remarks>
        static CustomIconProjectViewDrawer()
        {
            EditorApplication.projectWindowItemOnGUI += DrawCustomIcon;
        }

        /// <summary>
        /// Draws a custom icon for an asset in the Project view if one is assigned.
        /// </summary>
        /// <param name="guid">The GUID of the asset being drawn.</param>
        /// <param name="selectionRect">The rectangle area for the item in the Project view.</param>
        /// <remarks>
        /// This method is called for each item in the Project view and checks if
        /// a custom icon has been assigned to the asset. If so, it draws the custom
        /// icon over the default icon.
        /// </remarks>
        private static void DrawCustomIcon(string guid, Rect selectionRect)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

            if (obj == null || !IsSupportedType(obj))
                return;

            string iconKey = "CustomIcon_" + guid;
            string customIconPath = EditorPrefs.GetString(iconKey, "");

            if (string.IsNullOrEmpty(customIconPath))
                return;

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(customIconPath);
            if (icon != null)
            {
                Rect iconRect = new(selectionRect.x - 2, selectionRect.y - 1, 18, 18);

                if (selectionRect.height > 20)
                    iconRect = new(selectionRect.x - 2, selectionRect.y - 1, 66, 66);

                GUI.DrawTexture(iconRect, icon);
            }
        }

        /// <summary>
        /// Determines if an object type supports custom icons.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object type supports custom icons; otherwise, false.</returns>
        /// <remarks>
        /// Supports prefabs, ScriptableObjects, and TextAssets.
        /// This should match the supported types in the CustomIconEditor class.
        /// </remarks>
        private static bool IsSupportedType(Object obj)
        {
            if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
                return true;

            return obj is ScriptableObject or TextAsset;
        }
    }
}

#endif