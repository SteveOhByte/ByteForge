#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ByteForge.Editor;
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Enhanced inspector that combines button functionality, notes system, and property grouping features.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class EnhancedInspectorEditor : UnityEditor.Editor
    {
        #region Private Fields

        // Button System
        private List<ButtonComponents.MethodCache> buttonMethods = new();

        // Notes System
        private const string FoldoutPrefsKey = "InspectorNotes_Foldout_";
        private string currentNote = string.Empty;
        private bool showNoteField = false;
        private Vector2 scrollPos;
        private InspectorNotesData notesData;
        private bool noteSectionExpanded;

        // Group Management System
        private Dictionary<string, int> tabGroups = new();
        private Dictionary<string, bool> foldoutStates = new();
        private GroupDrawer groupDrawer;

        #endregion

        private void OnEnable()
        {
            // Cache button methods
            buttonMethods = ButtonComponents.ButtonMethodCache.GetMethodsForType(target.GetType());

            // Notes initialization
            InitializeNotesSystem();

            // Initialize group drawer
            groupDrawer = new(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw default inspector or grouped properties depending on if any grouping attributes exist
            if (groupDrawer.HasGroupingAttributes())
                groupDrawer.DrawGroupedInspector();
            else
                DrawDefaultInspector();

            // Draw button controls if any exist
            if (buttonMethods.Count > 0)
            {
                EditorGUILayout.Space();
                DrawButtons();
            }

            // Draw Notes Section if enabled
            if (notesData != null && !notesData.IgnoredTypes.Contains(target.GetType().FullName))
            {
                EditorGUILayout.Space();
                DrawNotesSection();
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region Button Methods

        private void DrawButtons()
        {
            foreach (ButtonComponents.MethodCache methodCache in buttonMethods)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (methodCache.Parameters.Length > 0)
                {
                    EditorGUILayout.LabelField(methodCache.DisplayName, EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;
                    for (int i = 0; i < methodCache.Parameters.Length; i++)
                    {
                        methodCache.ParameterValues[i] = ButtonComponents.ParameterDrawer.DrawParameterField(
                            methodCache.Parameters[i],
                            methodCache.ParameterValues[i]
                        );
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(2);
                }

                if (GUILayout.Button(methodCache.Parameters.Length > 0 ? "Execute" : methodCache.DisplayName))
                {
                    try
                    {
                        if (methodCache.Method.ReturnType == typeof(IEnumerator))
                        {
                            ((MonoBehaviour)target).StartCoroutine(
                                (IEnumerator)methodCache.Method.Invoke(target, methodCache.ParameterValues));
                        }
                        else
                        {
                            object result = methodCache.Method.Invoke(target, methodCache.ParameterValues);
                            if (result is not null)
                                BFDebug.Log($"Invocation of {methodCache.DisplayName} returned {result}");
                        }
                    }
                    catch (Exception e)
                    {
                        BFDebug.LogError(
                            $"Error invoking {methodCache.DisplayName}: {e.InnerException?.Message ?? e.Message}");
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        #endregion

        #region Notes Methods

        private void InitializeNotesSystem()
        {
            notesData = AssetDatabase.LoadAssetAtPath<InspectorNotesData>("Assets/ByteForge/InspectorNotesData.asset");
            if (notesData == null) return;

            string targetType = target.GetType().FullName;
            if (notesData.IgnoredTypes.Contains(targetType)) return;

            string noteKey = NotesUtility.GetNoteKey((MonoBehaviour)target);
            NoteEntry noteEntry = notesData.Notes.FirstOrDefault(n => n.GetUniqueKey() == noteKey);
            currentNote = noteEntry != null ? noteEntry.Note : string.Empty;
            noteSectionExpanded = EditorPrefs.GetBool(FoldoutPrefsKey + noteKey, true);
        }

        private void DrawNotesSection()
        {
            string noteKey = NotesUtility.GetNoteKey((MonoBehaviour)target);
            bool newExpanded = EditorGUILayout.Foldout(noteSectionExpanded, "Notes", true);
            if (newExpanded != noteSectionExpanded)
            {
                noteSectionExpanded = newExpanded;
                EditorPrefs.SetBool(FoldoutPrefsKey + noteKey, noteSectionExpanded);
            }

            if (!noteSectionExpanded)
                return;

            EditorGUI.indentLevel++;

            if (showNoteField)
                DrawNoteEditField();
            else
                DrawNoteDisplay();

            EditorGUI.indentLevel--;
        }

        private void DrawNoteEditField()
        {
            EditorGUILayout.LabelField("Edit Note:");

            GUIStyle textAreaStyle = new(EditorStyles.textArea) { wordWrap = true };
            float height = Mathf.Clamp(
                textAreaStyle.CalcHeight(new(currentNote), EditorGUIUtility.currentViewWidth - 40),
                50,
                150
            );

            currentNote = EditorGUILayout.TextArea(currentNote, textAreaStyle, GUILayout.Height(height));

            if (string.IsNullOrWhiteSpace(currentNote))
                EditorGUILayout.HelpBox("Note is empty!", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Note"))
            {
                SaveNote();
                showNoteField = false;
            }

            if (GUILayout.Button("Cancel"))
            {
                // Reset to the previously saved note if any
                string noteKey = NotesUtility.GetNoteKey((MonoBehaviour)target);
                NoteEntry noteEntry = notesData.Notes.FirstOrDefault(n => n.GetUniqueKey() == noteKey);
                currentNote = noteEntry != null ? noteEntry.Note : string.Empty;
                showNoteField = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNoteDisplay()
        {
            if (!string.IsNullOrEmpty(currentNote))
            {
                GUIStyle labelStyle = new(EditorStyles.wordWrappedLabel);
                float labelHeight = labelStyle.CalcHeight(
                    new(currentNote),
                    EditorGUIUtility.currentViewWidth - 40
                );
                float limitedLabelHeight = Mathf.Min(labelHeight, 150);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(limitedLabelHeight));
                EditorGUILayout.LabelField(currentNote, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Edit"))
                    showNoteField = true;
                if (GUILayout.Button("Delete"))
                    DeleteNote();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button("Add Note"))
                    showNoteField = true;
            }
        }

        private void SaveNote()
        {
            string noteKey = NotesUtility.GetNoteKey((MonoBehaviour)target);
            NoteEntry noteEntry = notesData.Notes.FirstOrDefault(n => n.GetUniqueKey() == noteKey);
            if (noteEntry != null)
                noteEntry.Note = currentNote;
            else
            {
                notesData.Notes.Add(new()
                {
                    GameObjectPath = NotesUtility.GetGameObjectPath(((MonoBehaviour)target).gameObject),
                    ComponentType = target.GetType().ToString(),
                    Note = currentNote
                });
            }

            EditorUtility.SetDirty(notesData);
            AssetDatabase.SaveAssets();
        }

        private void DeleteNote()
        {
            string noteKey = NotesUtility.GetNoteKey((MonoBehaviour)target);
            NoteEntry noteEntry = notesData.Notes.FirstOrDefault(n => n.GetUniqueKey() == noteKey);
            if (noteEntry != null)
            {
                notesData.Notes.Remove(noteEntry);
                EditorUtility.SetDirty(notesData);
                AssetDatabase.SaveAssets();
            }

            currentNote = "";
        }

        #endregion
    }
}
#endif