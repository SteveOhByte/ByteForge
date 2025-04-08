#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Handles the grouping and drawing of properties in the inspector.
    /// </summary>
    public class GroupDrawer
    {
        private SerializedObject serializedObject;
        private Dictionary<string, int> tabGroups = new();
        private Dictionary<string, bool> foldoutStates = new();

        // Cache to avoid checking all properties multiple times
        private bool? hasGroupingAttributes = null;

        public GroupDrawer(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
        }

        /// <summary>
        /// Checks if any property in the serialized object has grouping attributes.
        /// </summary>
        public bool HasGroupingAttributes()
        {
            if (hasGroupingAttributes.HasValue)
                return hasGroupingAttributes.Value;

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name.Equals("m_Script"))
                    continue;

                // Check if any property has grouping attributes
                if (PropertyReflectionUtility.GetAttribute<TabGroupAttribute>(iterator) != null ||
                    PropertyReflectionUtility.GetAttribute<GroupBoxAttribute>(iterator) != null ||
                    PropertyReflectionUtility.GetAttribute<FoldoutGroupAttribute>(iterator) != null)
                {
                    hasGroupingAttributes = true;
                    return true;
                }
            }

            hasGroupingAttributes = false;
            return false;
        }

        /// <summary>
        /// Draws the inspector with grouped properties.
        /// </summary>
        public void DrawGroupedInspector()
        {
            serializedObject.Update();

            // Group properties by tab/group structure
            Dictionary<string, Dictionary<string, List<SerializedProperty>>> tabGroupedProperties =
                GroupPropertiesByTabs();
            Dictionary<string, List<SerializedProperty>> nonTabProperties = GetNonTabProperties();

            // Draw script field if it exists
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(scriptProperty);
                EditorGUI.EndDisabledGroup();
            }

            // Draw non-tab properties first
            DrawPropertiesGrouped(nonTabProperties);

            // Draw tab groups
            foreach (KeyValuePair<string, Dictionary<string, List<SerializedProperty>>> tabGroup in
                     tabGroupedProperties)
                DrawTabGroup(tabGroup.Key, tabGroup.Value);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Groups properties by tab attributes.
        /// </summary>
        private Dictionary<string, Dictionary<string, List<SerializedProperty>>> GroupPropertiesByTabs()
        {
            Dictionary<string, Dictionary<string, List<SerializedProperty>>> result =
                new Dictionary<string, Dictionary<string, List<SerializedProperty>>>();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name.Equals("m_Script")) continue;

                // Check if property has TabGroup attribute
                TabGroupAttribute tabAttr = PropertyReflectionUtility.GetAttribute<TabGroupAttribute>(iterator);
                if (tabAttr != null)
                {
                    // Create tab group if it doesn't exist
                    if (!result.ContainsKey(tabAttr.TabGroupName))
                        result[tabAttr.TabGroupName] = new();

                    // Create tab if it doesn't exist
                    if (!result[tabAttr.TabGroupName].ContainsKey(tabAttr.TabName))
                        result[tabAttr.TabGroupName][tabAttr.TabName] = new();

                    // Add property to tab
                    result[tabAttr.TabGroupName][tabAttr.TabName].Add(iterator.Copy());
                }
            }

            return result;
        }

        /// <summary>
        /// Gets properties that don't have a tab attribute.
        /// </summary>
        private Dictionary<string, List<SerializedProperty>> GetNonTabProperties()
        {
            Dictionary<string, List<SerializedProperty>> result = new Dictionary<string, List<SerializedProperty>>();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name.Equals("m_Script")) continue;

                // Skip properties with TabGroup attribute
                TabGroupAttribute tabAttr = PropertyReflectionUtility.GetAttribute<TabGroupAttribute>(iterator);
                if (tabAttr != null) continue;

                // Check if property has GroupBox attribute
                GroupBoxAttribute groupAttr = PropertyReflectionUtility.GetAttribute<GroupBoxAttribute>(iterator);
                string groupName = groupAttr != null ? groupAttr.GroupName : "";

                // Create group if it doesn't exist
                if (!result.ContainsKey(groupName))
                    result[groupName] = new();

                // Add property to group
                result[groupName].Add(iterator.Copy());
            }

            return result;
        }

        /// <summary>
        /// Draws a tab group.
        /// </summary>
        private void DrawTabGroup(string tabGroupName, Dictionary<string, List<SerializedProperty>> tabs)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Initialize tab selection if needed
            tabGroups.TryAdd(tabGroupName, 0);

            // Get sorted tab names
            string[] tabNames = tabs.Keys.ToArray();

            // Draw tab buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(tabGroupName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < tabNames.Length; i++)
            {
                if (GUILayout.Toggle(tabGroups[tabGroupName] == i, tabNames[i], "Button", GUILayout.MinWidth(80)))
                    tabGroups[tabGroupName] = i;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Get current tab's properties
            string currentTabName = tabNames[tabGroups[tabGroupName]];
            List<SerializedProperty> tabProperties = tabs[currentTabName];

            // Group properties by GroupBox
            Dictionary<string, List<SerializedProperty>>
                groupedTabProperties = GroupPropertiesByGroupBox(tabProperties);

            // Draw the grouped properties
            DrawPropertiesGrouped(groupedTabProperties);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Groups properties by GroupBox attribute.
        /// </summary>
        private Dictionary<string, List<SerializedProperty>> GroupPropertiesByGroupBox(
            List<SerializedProperty> properties)
        {
            Dictionary<string, List<SerializedProperty>> result = new Dictionary<string, List<SerializedProperty>>();

            foreach (SerializedProperty property in properties)
            {
                GroupBoxAttribute groupAttr = PropertyReflectionUtility.GetAttribute<GroupBoxAttribute>(property);
                string groupName = groupAttr != null ? groupAttr.GroupName : "";

                if (!result.ContainsKey(groupName))
                    result[groupName] = new();

                result[groupName].Add(property);
            }

            return result;
        }

        /// <summary>
        /// Draws properties grouped by GroupBox attribute.
        /// </summary>
        private void DrawPropertiesGrouped(Dictionary<string, List<SerializedProperty>> groupedProperties)
        {
            foreach (KeyValuePair<string, List<SerializedProperty>> group in groupedProperties)
            {
                if (string.IsNullOrEmpty(group.Key))
                {
                    // Ungrouped properties
                    foreach (SerializedProperty property in group.Value)
                        DrawProperty(property);
                }
                else
                {
                    // Grouped properties
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);

                    // Further group by foldouts
                    Dictionary<string, List<SerializedProperty>> foldoutGrouped = GroupPropertiesByFoldout(group.Value);

                    // Draw properties by foldout groups
                    DrawFoldoutGroups(foldoutGrouped);

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
        }

        /// <summary>
        /// Groups properties by FoldoutGroup attribute.
        /// </summary>
        private Dictionary<string, List<SerializedProperty>> GroupPropertiesByFoldout(
            List<SerializedProperty> properties)
        {
            Dictionary<string, List<SerializedProperty>> result = new Dictionary<string, List<SerializedProperty>>();

            foreach (SerializedProperty property in properties)
            {
                FoldoutGroupAttribute foldoutAttr =
                    PropertyReflectionUtility.GetAttribute<FoldoutGroupAttribute>(property);
                string foldoutName = foldoutAttr != null ? foldoutAttr.FoldoutName : "";

                if (!result.ContainsKey(foldoutName))
                    result[foldoutName] = new();

                result[foldoutName].Add(property);
            }

            return result;
        }

        /// <summary>
        /// Draws properties grouped by FoldoutGroup attribute.
        /// </summary>
        private void DrawFoldoutGroups(Dictionary<string, List<SerializedProperty>> foldoutGrouped)
        {
            foreach (KeyValuePair<string, List<SerializedProperty>> foldout in foldoutGrouped)
            {
                if (string.IsNullOrEmpty(foldout.Key))
                {
                    // Non-foldout properties
                    foreach (SerializedProperty property in foldout.Value)
                        DrawProperty(property);
                }
                else
                {
                    // Foldout properties
                    if (!foldoutStates.ContainsKey(foldout.Key))
                        foldoutStates[foldout.Key] = true; // Default expanded

                    foldoutStates[foldout.Key] = EditorGUILayout.Foldout(foldoutStates[foldout.Key], foldout.Key, true);

                    if (foldoutStates[foldout.Key])
                    {
                        EditorGUI.indentLevel++;
                        foreach (SerializedProperty property in foldout.Value)
                            DrawProperty(property);
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        /// <summary>
        /// Draws a property with respect to its custom attributes.
        /// </summary>
        private void DrawProperty(SerializedProperty property)
        {
            // Get custom label if any
            LabelAttribute labelAttr = PropertyReflectionUtility.GetAttribute<LabelAttribute>(property);
            GUIContent label = labelAttr != null
                ? new(labelAttr.Label)
                : new GUIContent(property.displayName);

            if (PropertyReflectionUtility.GetAttribute<AssetsOnlyAttribute>(property) !=
                null) // Draw using AssetsOnly drawer
                DrawAssetsOnlyField(property, label);
            else if (PropertyReflectionUtility.GetAttribute<SceneObjectsOnlyAttribute>(property) !=
                     null) // Draw using SceneObjectsOnly drawer
                DrawSceneObjectsOnlyField(property, label);
            else // Draw normal property
                EditorGUILayout.PropertyField(property, label, true);
        }

        /// <summary>
        /// Draws a property with the AssetsOnly attribute.
        /// </summary>
        private void DrawAssetsOnlyField(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                EditorGUI.BeginChangeCheck();
                FieldInfo fieldInfo = PropertyReflectionUtility.GetFieldInfo(property);
                UnityEngine.Object obj =
                    EditorGUILayout.ObjectField(label, property.objectReferenceValue, fieldInfo.FieldType, false);

                if (EditorGUI.EndChangeCheck())
                {
                    // Validate that it's not a scene object
                    if (obj == null || !EditorUtility.IsPersistent(obj))
                        BFDebug.LogWarning("Only project assets are allowed for this field.");
                    else
                        property.objectReferenceValue = obj;
                }
            }
            else
                EditorGUILayout.PropertyField(property, label, true);
        }

        /// <summary>
        /// Draws a property with the SceneObjectsOnly attribute.
        /// </summary>
        private void DrawSceneObjectsOnlyField(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                EditorGUI.BeginChangeCheck();
                FieldInfo fieldInfo = PropertyReflectionUtility.GetFieldInfo(property);
                UnityEngine.Object obj =
                    EditorGUILayout.ObjectField(label, property.objectReferenceValue, fieldInfo.FieldType, true);

                if (EditorGUI.EndChangeCheck())
                {
                    // Validate that it's a scene object
                    if (obj == null || EditorUtility.IsPersistent(obj))
                        BFDebug.LogWarning("Only scene objects are allowed for this field.");
                    else
                        property.objectReferenceValue = obj;
                }
            }
            else
                EditorGUILayout.PropertyField(property, label, true);
        }
    }
}
#endif