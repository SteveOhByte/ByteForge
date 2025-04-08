#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    [CustomEditor(typeof(DefaultAsset))]
    public class CSVInspector : UnityEditor.Editor
    {
        private List<List<string>> csvData = new();
        private string errorMessage;
        private Vector2 scrollPos;
        private string filePath;
        private bool isCSV;
        private new bool hasUnsavedChanges = false;
        private bool isSaving = false;
        private List<string> columnNames = new();

        private void OnEnable()
        {
            filePath = AssetDatabase.GetAssetPath(target);
            isCSV = filePath.EndsWith(".csv");

            if (isCSV) LoadCSV();
        }

        private void OnDisable()
        {
            if (!hasUnsavedChanges || isSaving) return;

            bool saveNow = EditorUtility.DisplayDialog("Unsaved Changes",
                "You have unsaved changes in the CSV file. Do you want to save before exiting?",
                "Save", "Discard");

            if (saveNow) SaveCSV();
        }

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

        private void AddRow()
        {
            csvData.Add(new(new string[columnNames.Count]));
            hasUnsavedChanges = true;
        }

        private void RemoveRow(int index)
        {
            csvData.RemoveAt(index);
            hasUnsavedChanges = true;
        }

        private void AddColumn()
        {
            columnNames.Add($"Column {columnNames.Count + 1}");

            foreach (List<string> row in csvData)
                row.Add("");
            hasUnsavedChanges = true;
        }

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