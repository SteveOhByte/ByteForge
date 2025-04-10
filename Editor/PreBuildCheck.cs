#if UNITY_EDITOR
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ByteForge.Editor
{
    /// <summary>
    /// Pre-build validation class that checks for unsafe editor code usage before build.
    /// </summary>
    /// <remarks>
    /// This class implements Unity's IPreprocessBuildWithReport interface to run validation 
    /// before the build process starts. It scans scripts for any usage of UnityEditor
    /// namespace that isn't properly wrapped in #if UNITY_EDITOR conditionals,
    /// which could cause build failures in runtime builds.
    /// </remarks>
    public class PreBuildCheck : IPreprocessBuildWithReport
    {
        /// <summary>
        /// Gets the callback order for this build preprocessor.
        /// </summary>
        /// <remarks>
        /// Lower values get executed first. Zero is used as a default
        /// to run this check early in the build pipeline.
        /// </remarks>
        public int callbackOrder => 0;

        /// <summary>
        /// Executes before the build process begins to check for unsafe editor code.
        /// </summary>
        /// <param name="report">The build report containing information about the build.</param>
        /// <exception cref="BuildFailedException">Thrown when unsafe editor code is detected.</exception>
        /// <remarks>
        /// This method checks if any scripts contain unsafe editor code, displays a warning dialog
        /// listing the problematic files, and then fails the build with an exception.
        /// </remarks>
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

        /// <summary>
        /// Checks for any unsafe editor code in script files.
        /// </summary>
        /// <returns>
        /// A tuple containing: 
        /// - A boolean indicating if any problem was found
        /// - An array of file paths with unsafe editor code
        /// </returns>
        /// <remarks>
        /// Searches for script files in the main editor folder and checks each one
        /// for UnityEditor usage that isn't properly wrapped in conditional compilation blocks.
        /// </remarks>
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

        /// <summary>
        /// Determines if a specific file contains unsafe editor code.
        /// </summary>
        /// <param name="filePath">The path to the file to check.</param>
        /// <returns>True if the file contains unsafe editor code; otherwise, false.</returns>
        /// <remarks>
        /// Reads the file line by line and checks if any line contains "UnityEditor"
        /// outside of a #if UNITY_EDITOR conditional block. Commented lines are ignored.
        /// </remarks>
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