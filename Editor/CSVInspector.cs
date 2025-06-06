#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Custom inspector for CSV files in the Unity editor.
    /// </summary>
    /// <remarks>
    /// Provides a rich editor interface for viewing and editing CSV files directly
    /// within the Unity inspector. Allows editing cells, adding/removing rows and columns,
    /// and saving changes back to the file.
    /// 
    /// This editor is applied to all DefaultAsset files with a .csv extension.
    /// </remarks>
    [CustomEditor(typeof(DefaultAsset))]
    public class CSVInspector : UnityEditor.Editor
    {
        /// <summary>
        /// The parsed data from the CSV file.
        /// </summary>
        private List<List<string>> csvData = new();
        
        /// <summary>
        /// Error message to display if the CSV file cannot be loaded or parsed.
        /// </summary>
        private string errorMessage;
        
        /// <summary>
        /// Scroll position for the CSV table view.
        /// </summary>
        private Vector2 scrollPos;
        
        /// <summary>
        /// The file path of the CSV file being edited.
        /// </summary>
        private string filePath;
        
        /// <summary>
        /// Whether the target asset is a CSV file.
        /// </summary>
        private bool isCSV;
        
        /// <summary>
        /// Whether there are unsaved changes to the CSV data.
        /// </summary>
        private new bool hasUnsavedChanges = false;
        
        /// <summary>
        /// Whether the CSV is currently being saved.
        /// </summary>
        /// <remarks>
        /// Used to prevent showing the unsaved changes dialogue during save operations.
        /// </remarks>
        private bool isSaving = false;
        
        /// <summary>
        /// The column names/headers from the CSV file.
        /// </summary>
        private List<string> columnNames = new();

        /// <summary>
        /// Called when the inspector becomes enabled.
        /// </summary>
        /// <remarks>
        /// Checks if the target asset is a CSV file and loads its content if it is.
        /// </remarks>
        private void OnEnable()
        {
            filePath = AssetDatabase.GetAssetPath(target);
            isCSV = filePath.EndsWith(".csv");

            if (isCSV) LoadCSV();
        }

        /// <summary>
        /// Called when the inspector becomes disabled.
        /// </summary>
        /// <remarks>
        /// Checks for unsaved changes and prompts the user to save if necessary.
        /// </remarks>
        private void OnDisable()
        {
            if (!hasUnsavedChanges || isSaving) return;

            bool saveNow = EditorUtility.DisplayDialog("Unsaved Changes",
                "You have unsaved changes in the CSV file. Do you want to save before exiting?",
                "Save", "Discard");

            if (saveNow) SaveCSV();
        }

        /// <summary>
        /// Draws the inspector GUI for the CSV file.
        /// </summary>
        /// <remarks>
        /// If the target is not a CSV file, draws the default inspector.
        /// Otherwise, displays a custom editor with a spreadsheet-like interface
        /// for editing the CSV data.
        /// </remarks>
        public override void OnInspectorGUI()
        {
            if (!isCSV)
            {
                DrawDefaultInspector();
                return;
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                return;
            }

            EditorGUILayout.Space();
            GUI.enabled = hasUnsavedChanges;
            if (GUILayout.Button("Save CSV", GUILayout.Height(25)))
            {
                isSaving = true;
                SaveCSV();
                isSaving = false;
            }

            GUI.enabled = true;
            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Draw Column Headers
            if (csvData.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < columnNames.Count; i++)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(120));

                    // Editable Column Name
                    string newColumnName = EditorGUILayout.TextField(columnNames[i], EditorStyles.boldLabel);
                    if (newColumnName != columnNames[i])
                    {
                        columnNames[i] = newColumnName;
                        hasUnsavedChanges = true;
                    }

                    // Remove Column Button (Only if more than 1 column)
                    if (columnNames.Count > 1)
                    {
                        if (GUILayout.Button("-", GUILayout.Width(25)))
                        {
                            RemoveColumn(i);
                            return;
                        }
                    }

                    EditorGUILayout.EndVertical();
                }

                // Add Column Button
                if (GUILayout.Button("+", GUILayout.Width(30))) AddColumn();
                EditorGUILayout.EndHorizontal();
            }

            // Draw Table Data
            for (int i = 0; i < csvData.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < csvData[i].Count; j++)
                {
                    string newValue = EditorGUILayout.TextField(csvData[i][j], GUILayout.Width(120));
                    if (newValue == csvData[i][j]) continue;

                    csvData[i][j] = newValue;
                    hasUnsavedChanges = true;
                }

                // Remove Row Button
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    RemoveRow(i);
                    return;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // Add Row Button
            if (GUILayout.Button("Add Row", GUILayout.Height(25))) AddRow();

            EditorGUILayout.Space();
        }

        /// <summary>
        /// Loads and parses the CSV file data.
        /// </summary>
        /// <remarks>
        /// Reads the CSV file, parses it into a list of rows and columns,
        /// and extracts the column headers from the first row.
        /// Sets an error message if the file cannot be read or parsed.
        /// </remarks>
        private void LoadCSV()
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length == 0) return;

                csvData = lines.Skip(1) // Skip header row
                    .Select(line => line.Split(',').ToList())
                    .ToList();

                // Load column names from the first row
                columnNames = lines[0].Split(',').ToList();

                errorMessage = null;
                hasUnsavedChanges = false;
            }
            catch (System.Exception e)
            {
                errorMessage = "Failed to read CSV: " + e.Message;
                csvData = null;
            }
        }

        /// <summary>
        /// Saves the current CSV data back to the file.
        /// </summary>
        /// <remarks>
        /// Converts the column names and data rows back to CSV format
        /// and writes them to the file. Refreshes the AssetDatabase
        /// to ensure Unity recognizes the changes.
        /// </remarks>
        private void SaveCSV()
        {
            try
            {
                List<string> lines = new()
                {
                    string.Join(",", columnNames) // Save column names as first row
                };

                lines.AddRange(csvData.Select(row => string.Join(",", row)));
                File.WriteAllLines(filePath, lines);
                AssetDatabase.Refresh();
                hasUnsavedChanges = false;
            }
            catch (System.Exception e)
            {
                BFDebug.LogError("Failed to save CSV: " + e.Message);
            }
        }

        /// <summary>
        /// Adds a new empty row to the CSV data.
        /// </summary>
        /// <remarks>
        /// Creates a row with empty strings for each column and marks the data as changed.
        /// </remarks>
        private void AddRow()
        {
            csvData.Add(new(new string[columnNames.Count]));
            hasUnsavedChanges = true;
        }

        /// <summary>
        /// Removes a row from the CSV data.
        /// </summary>
        /// <param name="index">The index of the row to remove.</param>
        /// <remarks>
        /// Removes the specified row and marks the data as changed.
        /// </remarks>
        private void RemoveRow(int index)
        {
            csvData.RemoveAt(index);
            hasUnsavedChanges = true;
        }

        /// <summary>
        /// Adds a new column to the CSV data.
        /// </summary>
        /// <remarks>
        /// Adds a column header with a default name and adds an empty string
        /// to each row for the new column. Marks the data as changed.
        /// </remarks>
        private void AddColumn()
        {
            columnNames.Add($"Column {columnNames.Count + 1}");

            foreach (List<string> row in csvData)
                row.Add("");
            hasUnsavedChanges = true;
        }

        /// <summary>
        /// Removes a column from the CSV data.
        /// </summary>
        /// <param name="columnIndex">The index of the column to remove.</param>
        /// <remarks>
        /// Removes the specified column header and the corresponding column from each row.
        /// Does nothing if there is only one column. Marks the data as changed.
        /// </remarks>
        private void RemoveColumn(int columnIndex)
        {
            if (columnNames.Count <= 1) return;

            columnNames.RemoveAt(columnIndex);

            foreach (List<string> row in csvData.Where(row => columnIndex < row.Count))
                row.RemoveAt(columnIndex);

            hasUnsavedChanges = true;
        }
    }
}
#endif