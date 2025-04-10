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
    /// Handles the organization and display of properties in the Unity inspector.
    /// </summary>
    /// <remarks>
    /// This class provides advanced inspector customization, allowing properties to be
    /// organized into tabs, group boxes, and foldouts based on attributes applied to
    /// the fields. It handles the grouping logic and drawing of the custom inspector UI.
    /// </remarks>
    public class GroupDrawer
    {
        /// <summary>
        /// The SerializedObject being drawn in the inspector.
        /// </summary>
        private SerializedObject serializedObject;
        
        /// <summary>
        /// Dictionary tracking the currently selected tab in each tab group.
        /// </summary>
        private Dictionary<string, int> tabGroups = new();
        
        /// <summary>
        /// Dictionary tracking the expanded/collapsed state of each foldout group.
        /// </summary>
        private Dictionary<string, bool> foldoutStates = new();

        /// <summary>
        /// Cache to avoid repeated attribute checking.
        /// </summary>
        /// <remarks>
        /// Nullable boolean that stores whether any property has grouping attributes,
        /// avoiding the need to scan all properties multiple times.
        /// </remarks>
        private bool? hasGroupingAttributes = null;

        /// <summary>
        /// Initializes a new instance of the GroupDrawer class.
        /// </summary>
        /// <param name="serializedObject">The SerializedObject to draw in the inspector.</param>
        public GroupDrawer(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
        }

        /// <summary>
        /// Determines if any property in the serialized object has grouping attributes.
        /// </summary>
        /// <returns>
        /// True if at least one property has a TabGroup, GroupBox, or FoldoutGroup attribute;
        /// otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method uses caching to improve performance when called multiple times.
        /// </remarks>
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
        /// Draws the complete inspector with grouped properties.
        /// </summary>
        /// <remarks>
        /// This is the main entry point for drawing the custom inspector.
        /// It organizes properties into tabs and groups, then draws them accordingly.
        /// </remarks>
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
        /// <returns>
        /// A nested dictionary where the outer key is the tab group name,
        /// the inner key is the tab name, and the value is a list of properties in that tab.
        /// </returns>
        /// <remarks>
        /// This method scans all properties and organizes those with TabGroup attributes
        /// into their respective tab groups and tabs.
        /// </remarks>
        private Dictionary<string, Dictionary<string, List<SerializedProperty>>> GroupPropertiesByTabs()
        {
            Dictionary<string, Dictionary<string, List<SerializedProperty>>> result = new();

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
        /// <returns>
        /// A dictionary where the key is the group name (empty string for ungrouped properties),
        /// and the value is a list of properties in that group.
        /// </returns>
        /// <remarks>
        /// This method collects all properties that don't have a TabGroup attribute,
        /// and organizes them by GroupBox attribute if they have one.
        /// </remarks>
        private Dictionary<string, List<SerializedProperty>> GetNonTabProperties()
        {
            Dictionary<string, List<SerializedProperty>> result = new();

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
        /// Draws a tab group with selectable tabs.
        /// </summary>
        /// <param name="tabGroupName">The name of the tab group.</param>
        /// <param name="tabs">A dictionary of tab names and their properties.</param>
        /// <remarks>
        /// This method draws a tab group with buttons for each tab, and displays
        /// the properties of the currently selected tab.
        /// </remarks>
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
        /// <param name="properties">The list of properties to group.</param>
        /// <returns>
        /// A dictionary where the key is the group name (empty string for ungrouped properties),
        /// and the value is a list of properties in that group.
        /// </returns>
        /// <remarks>
        /// This method organizes properties based on their GroupBox attributes.
        /// </remarks>
        private Dictionary<string, List<SerializedProperty>> GroupPropertiesByGroupBox(
            List<SerializedProperty> properties)
        {
            Dictionary<string, List<SerializedProperty>> result = new();

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
        /// <param name="groupedProperties">A dictionary of group names and their properties.</param>
        /// <remarks>
        /// This method draws properties organized by group, with each group
        /// displayed in a styled box with a header.
        /// </remarks>
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
        /// <param name="properties">The list of properties to group.</param>
        /// <returns>
        /// A dictionary where the key is the foldout name (empty string for non-foldout properties),
        /// and the value is a list of properties in that foldout.
        /// </returns>
        /// <remarks>
        /// This method organizes properties based on their FoldoutGroup attributes.
        /// </remarks>
        private Dictionary<string, List<SerializedProperty>> GroupPropertiesByFoldout(
            List<SerializedProperty> properties)
        {
            Dictionary<string, List<SerializedProperty>> result = new();

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
        /// <param name="foldoutGrouped">A dictionary of foldout names and their properties.</param>
        /// <remarks>
        /// This method draws properties organized by foldout, with each foldout
        /// having an expandable/collapsible header.
        /// </remarks>
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
        /// <param name="property">The property to draw.</param>
        /// <remarks>
        /// This method handles drawing a property, taking into account any custom
        /// attributes that affect how it should be displayed in the inspector.
        /// </remarks>
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
        /// <param name="property">The property to draw.</param>
        /// <param name="label">The label to display.</param>
        /// <remarks>
        /// This method draws a property field that only accepts project assets (not scene objects)
        /// and displays a warning if the user tries to assign a scene object.
        /// </remarks>
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
        /// <param name="property">The property to draw.</param>
        /// <param name="label">The label to display.</param>
        /// <remarks>
        /// This method draws a property field that only accepts scene objects (not project assets)
        /// and displays a warning if the user tries to assign a project asset.
        /// </remarks>
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