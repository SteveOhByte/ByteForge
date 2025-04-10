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
    /// Enhanced Unity inspector that combines button functionality, notes system, and property grouping features.
    /// </summary>
    /// <remarks>
    /// This custom editor extends Unity's default inspector for all MonoBehaviours with three main features:
    /// 1. Button System - Allows method execution directly from the inspector using attributes
    /// 2. Notes System - Enables developers to attach persistent notes to components
    /// 3. Group Management - Supports organizing properties into tabs, groups, and foldouts
    /// 
    /// The editor automatically detects which features are applicable for each component
    /// and displays the relevant controls.
    /// </remarks>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class EnhancedInspectorEditor : UnityEditor.Editor
    {
        #region Private Fields

        /// <summary>
        /// Cache of methods marked with button attributes in the target component.
        /// </summary>
        private List<ButtonComponents.MethodCache> buttonMethods = new();

        /// <summary>
        /// EditorPrefs key prefix for storing note foldout states.
        /// </summary>
        private const string FoldoutPrefsKey = "InspectorNotes_Foldout_";
        
        /// <summary>
        /// The current content of the note for this component.
        /// </summary>
        private string currentNote = string.Empty;
        
        /// <summary>
        /// Whether the note editing field is currently displayed.
        /// </summary>
        private bool showNoteField = false;
        
        /// <summary>
        /// The scroll position for the note display area.
        /// </summary>
        private Vector2 scrollPos;
        
        /// <summary>
        /// Reference to the asset containing all saved notes.
        /// </summary>
        private InspectorNotesData notesData;
        
        /// <summary>
        /// Whether the notes section is expanded in the inspector.
        /// </summary>
        private bool noteSectionExpanded;

        /// <summary>
        /// Dictionary tracking the currently selected tab in each tab group.
        /// </summary>
        private Dictionary<string, int> tabGroups = new();
        
        /// <summary>
        /// Dictionary tracking the expanded/collapsed state of each foldout group.
        /// </summary>
        private Dictionary<string, bool> foldoutStates = new();
        
        /// <summary>
        /// The drawer responsible for handling grouped properties display.
        /// </summary>
        private GroupDrawer groupDrawer;

        #endregion

        /// <summary>
        /// Initializes the editor when it becomes enabled.
        /// </summary>
        /// <remarks>
        /// This method caches button methods, sets up the notes system,
        /// and initializes the group drawer for organizing properties.
        /// </remarks>
        private void OnEnable()
        {
            // Cache button methods
            buttonMethods = ButtonComponents.ButtonMethodCache.GetMethodsForType(target.GetType());

            // Notes initialization
            InitializeNotesSystem();

            // Initialize group drawer
            groupDrawer = new(serializedObject);
        }

        /// <summary>
        /// Draws the custom inspector GUI.
        /// </summary>
        /// <remarks>
        /// This method handles the rendering of the entire inspector, including:
        /// - Standard or grouped property fields
        /// - Button controls for method execution
        /// - Notes system interface
        /// 
        /// Each section is drawn only if relevant for the current component.
        /// </remarks>
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

        /// <summary>
        /// Draws the button controls for methods marked with button attributes.
        /// </summary>
        /// <remarks>
        /// For each method with a button attribute, this draws:
        /// - A button with the method name or custom label
        /// - Parameter fields if the method has parameters
        /// - Error handling for method execution
        /// 
        /// It supports both regular methods and coroutines, and displays
        /// return values if the method returns a non-null result.
        /// </remarks>
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

        /// <summary>
        /// Initializes the notes system for the current component.
        /// </summary>
        /// <remarks>
        /// This method loads the notes data asset, checks if the current component type
        /// is allowed to have notes, and retrieves any existing note for this component.
        /// It also restores the expanded/collapsed state of the notes section from EditorPrefs.
        /// </remarks>
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

        /// <summary>
        /// Draws the notes section of the inspector.
        /// </summary>
        /// <remarks>
        /// This method handles the foldout header for the notes section
        /// and calls the appropriate method to draw either the note display
        /// or the note editing interface based on the current state.
        /// </remarks>
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

        /// <summary>
        /// Draws the note editing interface.
        /// </summary>
        /// <remarks>
        /// This method displays a text area for editing the note content
        /// and buttons to save or cancel the changes. The text area automatically
        /// adjusts its height based on the content while staying within reasonable limits.
        /// </remarks>
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

        /// <summary>
        /// Draws the note display interface.
        /// </summary>
        /// <remarks>
        /// This method shows the current note content in a scrollable area
        /// and provides buttons to edit or delete the note. If no note exists,
        /// it displays a button to add a new note.
        /// </remarks>
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

        /// <summary>
        /// Saves the current note to the notes data asset.
        /// </summary>
        /// <remarks>
        /// This method either updates an existing note or creates a new one,
        /// then marks the notes data asset as dirty and saves it to persist
        /// the changes between editor sessions.
        /// </remarks>
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

        /// <summary>
        /// Deletes the note for the current component.
        /// </summary>
        /// <remarks>
        /// This method removes the note entry from the notes data asset,
        /// marks the asset as dirty, and saves it to persist the changes.
        /// It also clears the current note content.
        /// </remarks>
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