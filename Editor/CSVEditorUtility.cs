#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    public static class CSVEditorUtility
    {
        private const string defaultCSVContent = "Column1,Column2,Column3\nValue1,Value2,Value3";

        [MenuItem("Assets/Create/CSV File", false, 50)]
        public static void CreateCSVFile()
        {
            string path = GetSelectedPathOrFallback();
            string filePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, "NewCSV.csv"));

            File.WriteAllText(filePath, defaultCSVContent, Encoding.UTF8);
            AssetDatabase.Refresh();

            Object asset = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            Selection.activeObject = asset;
            EditorApplication.delayCall += () => EditorGUIUtility.PingObject(asset);
        }

        private static string GetSelectedPathOrFallback()
        {
            if (Selection.activeObject == null) return "Assets";

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path))
                return Directory.Exists(path) ? path : Path.GetDirectoryName(path);

            return "Assets";
        }
    }
}
#endif