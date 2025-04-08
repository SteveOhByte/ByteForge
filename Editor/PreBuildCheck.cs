#if UNITY_EDITOR
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ByteForge.Editor
{
    public class PreBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            (bool problemFound, string[] problemFiles) = CheckForUnsafeEditorCode();
            if (!problemFound) return;
            
            StringBuilder message = new();
            message.AppendLine("Unsafe Editor code detected in the following files:");
            foreach (string file in problemFiles)
            {
                message.AppendLine(file);
            }

            message.AppendLine("Please wrap any usage of UnityEditor in #if UNITY_EDITOR blocks.");
            EditorUtility.DisplayDialog("Build Warning", message.ToString(), "OK");

            throw new BuildFailedException("Editor code issues require resolution.");
        }

        private static (bool, string[]) CheckForUnsafeEditorCode()
        {
            const string mainEditorFolderPath = "Assets/6 - Editor";

            // Get the current file name
            string currentFileName = System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name + ".cs";

            // Find files directly within the main Editor folder
            string[] problemFiles = AssetDatabase.FindAssets("t:Script", new[] { mainEditorFolderPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(filePath => HasUnsafeEditorCode(filePath) && !filePath.EndsWith(currentFileName))
                .ToArray();

            return (problemFiles.Length > 0, problemFiles);
        }

        private static bool HasUnsafeEditorCode(string filePath)
        {
            string[] lines = System.IO.File.ReadAllLines(filePath);

            // Check for any usage of UnityEditor outside of #if UNITY_EDITOR blocks
            bool inEditorBlock = false;
            foreach (string line in lines)
            {
                string trimmedLine = line.TrimStart();

                // Ignore commented lines
                if (trimmedLine.StartsWith("//"))
                {
                    continue;
                }

                if (trimmedLine.Contains("#if UNITY_EDITOR"))
                {
                    inEditorBlock = true;
                }
                else if (trimmedLine.Contains("#endif"))
                {
                    inEditorBlock = false;
                }
                else if (trimmedLine.Contains("UnityEditor") && !inEditorBlock)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif