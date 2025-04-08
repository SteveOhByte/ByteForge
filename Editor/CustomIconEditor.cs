#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ByteFroge.Editor
{
    public class CustomIconEditor : EditorWindow
    {
        private const string ICON_PREFS_PREFIX = "CustomIcon_";
        private Object selectedObject;
        private Texture2D customIcon;
        private Vector2 scrollPosition;

        private static readonly HashSet<string> supportedTypes = new()
        {
            "ScriptableObject",
            "TextAsset",
            "GameObject"
        };

        [MenuItem("Assets/Set Custom Icon")]
        public static void ShowWindow()
        {
            GetWindow<CustomIconEditor>("Custom Icons");
        }

        private void OnEnable()
        {
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
        }

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

        private Texture GetCustomIcon(Object obj)
        {
            string iconKey = ICON_PREFS_PREFIX + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            string iconPath = EditorPrefs.GetString(iconKey, "");

            if (string.IsNullOrEmpty(iconPath))
                return EditorGUIUtility.FindTexture("DefaultAsset Icon");

            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        }

        private void SetCustomIcon(Object obj, Texture icon)
        {
            string iconKey = ICON_PREFS_PREFIX + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            string iconPath = AssetDatabase.GetAssetPath(icon);
            EditorPrefs.SetString(iconKey, iconPath);

            // Force Unity to refresh the icon
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        private void ResetIcon(Object obj)
        {
            string iconKey = ICON_PREFS_PREFIX + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            EditorPrefs.DeleteKey(iconKey);
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

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