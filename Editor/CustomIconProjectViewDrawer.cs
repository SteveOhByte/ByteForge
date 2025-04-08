#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    [InitializeOnLoad]
    public class CustomIconProjectViewDrawer
    {
        static CustomIconProjectViewDrawer()
        {
            EditorApplication.projectWindowItemOnGUI += DrawCustomIcon;
        }

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

        private static bool IsSupportedType(Object obj)
        {
            if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
                return true;

            return obj is ScriptableObject or TextAsset;
        }
    }
}

#endif