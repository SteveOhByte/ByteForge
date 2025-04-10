#if UNITY_EDITOR
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Utility class for creating and managing CSV files in the Unity editor.
    /// </summary>
    /// <remarks>
    /// Provides functionality to create new CSV files with default content
    /// through the Unity editor's asset creation menu.
    /// </remarks>
    [SuppressMessage("Domain reload", "UDR0005:Domain Reload Analyzer")]
    public static class CSVEditorUtility
    {
        /// <summary>
        /// Default content used when creating a new CSV file.
        /// </summary>
        private const string defaultCSVContent = "Column1,Column2,Column3\nValue1,Value2,Value3";

        /// <summary>
        /// Creates a new CSV file in the selected folder in the Project window.
        /// </summary>
        /// <remarks>
        /// This method is registered as a menu item in the "Assets/Create" menu.
        /// When invoked, it creates a new CSV file with default content in the currently
        /// selected folder, or in the Assets folder if no folder is selected.
        /// After creating the file, it selects and highlights it in the Project window.
        /// </remarks>
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

        /// <summary>
        /// Gets the path of the currently selected folder in the Project window.
        /// </summary>
        /// <returns>
        /// The path of the selected folder, or "Assets" if no folder is selected.
        /// </returns>
        /// <remarks>
        /// If a file is selected, returns the directory containing that file.
        /// If nothing is selected or the selection is not a valid path, returns "Assets".
        /// </remarks>
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