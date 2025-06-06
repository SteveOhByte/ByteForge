#if UNITY_EDITOR
using System;
using System.Reflection;
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ByteForge.Editor
{
    /// <summary>
    /// Custom property drawer for the Label attribute.
    /// </summary>
    /// <remarks>
    /// This drawer changes the display name of a field in the inspector
    /// using the custom label provided in the LabelAttribute.
    /// </remarks>
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws the GUI for the property with a custom label.
        /// </summary>
        /// <param name="position">The position to draw the property.</param>
        /// <param name="property">The serialized property being drawn.</param>
        /// <param name="label">The default label of the property.</param>
        /// <remarks>
        /// Replaces the default label text with the custom text specified in the LabelAttribute.
        /// </remarks>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LabelAttribute labelAttr = attribute as LabelAttribute;
            label.text = labelAttr.Label;
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    /// <summary>
    /// Custom property drawer for the AssetsOnly attribute.
    /// </summary>
    /// <remarks>
    /// This drawer validates that an object reference field only accepts
    /// project assets (from Assets folder) and not scene objects.
    /// </remarks>
    [CustomPropertyDrawer(typeof(AssetsOnlyAttribute))]
    public class AssetsOnlyPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws the GUI for the property with asset validation.
        /// </summary>
        /// <param name="position">The position to draw the property.</param>
        /// <param name="property">The serialized property being drawn.</param>
        /// <param name="label">The label of the property.</param>
        /// <remarks>
        /// Displays a warning and prevents assignment if the user attempts to
        /// assign a scene object instead of a project asset.
        /// </remarks>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                EditorGUI.BeginChangeCheck();
                Object obj = EditorGUI.ObjectField(position, label, property.objectReferenceValue, fieldInfo.FieldType,
                    false);

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
                EditorGUI.PropertyField(position, property, label);

            EditorGUI.EndProperty();
        }
    }

    /// <summary>
    /// Custom property drawer for the SceneObjectsOnly attribute.
    /// </summary>
    /// <remarks>
    /// This drawer validates that an object reference field only accepts
    /// scene objects and not project assets (from Assets folder).
    /// </remarks>
    [CustomPropertyDrawer(typeof(SceneObjectsOnlyAttribute))]
    public class SceneObjectsOnlyPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws the GUI for the property with scene object validation.
        /// </summary>
        /// <param name="position">The position to draw the property.</param>
        /// <param name="property">The serialized property being drawn.</param>
        /// <param name="label">The label of the property.</param>
        /// <remarks>
        /// Displays a warning and prevents assignment if the user attempts to
        /// assign a project asset instead of a scene object.
        /// </remarks>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                EditorGUI.BeginChangeCheck();
                Object obj = EditorGUI.ObjectField(position, label, property.objectReferenceValue, fieldInfo.FieldType,
                    true);

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
                EditorGUI.PropertyField(position, property, label);

            EditorGUI.EndProperty();
        }
    }

    /// <summary>
    /// Utility class for property reflection operations.
    /// </summary>
    /// <remarks>
    /// Provides methods to extract information about serialized properties
    /// and their underlying field information using reflection.
    /// </remarks>
    public static class PropertyReflectionUtility
    {
        /// <summary>
        /// Gets a specific attribute from a SerializedProperty.
        /// </summary>
        /// <typeparam name="T">The type of attribute to retrieve.</typeparam>
        /// <param name="property">The serialized property to get the attribute from.</param>
        /// <returns>The first attribute of the specified type, or null if not found.</returns>
        /// <remarks>
        /// Uses reflection to find the field info for the property and then
        /// retrieves the first attribute of the specified type from that field.
        /// </remarks>
        public static T GetAttribute<T>(SerializedProperty property) where T : PropertyAttribute
        {
            FieldInfo fieldInfo = GetFieldInfo(property);
            if (fieldInfo == null) return null;

            return fieldInfo.GetCustomAttributes(typeof(T), true).Length > 0
                ? (T)fieldInfo.GetCustomAttributes(typeof(T), true)[0]
                : null;
        }

        /// <summary>
        /// Gets the FieldInfo for a SerializedProperty.
        /// </summary>
        /// <param name="property">The serialized property to get field info for.</param>
        /// <returns>The FieldInfo object representing the field, or null if not found.</returns>
        /// <remarks>
        /// Navigates through the property path to find the correct field info,
        /// handling nested properties and array elements.
        /// </remarks>
        public static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            Object targetObject = property.serializedObject.targetObject;
            Type targetType = targetObject.GetType();

            // Parse property path
            string path = property.propertyPath.Replace(".Array.data[", "[");
            string[] pathParts = path.Split('.');

            // Navigate through path
            FieldInfo field = null;
            foreach (string part in pathParts)
            {
                // Handle arrays
                if (part.Contains("["))
                {
                    int bracketIndex = part.IndexOf("[", StringComparison.Ordinal);
                    string fieldName = part[..bracketIndex];
                    int index = int.Parse(part.Substring(bracketIndex + 1, part.Length - bracketIndex - 2));

                    field = targetType.GetField(fieldName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field == null) return null;

                    // Skip array elements, the array field info is needed
                    targetType = field.FieldType.GetElementType() ?? field.FieldType.GetGenericArguments()[0];
                }
                else
                {
                    field = targetType.GetField(part,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field == null) return null;

                    targetType = field.FieldType;
                }
            }

            return field;
        }
    }
}
#endif