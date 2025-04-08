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
    /// Custom drawer for the Label attribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LabelAttribute labelAttr = attribute as LabelAttribute;
            label.text = labelAttr.Label;
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    /// <summary>
    /// Custom drawer for the AssetsOnly attribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(AssetsOnlyAttribute))]
    public class AssetsOnlyPropertyDrawer : PropertyDrawer
    {
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
    /// Custom drawer for the SceneObjectsOnly attribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneObjectsOnlyAttribute))]
    public class SceneObjectsOnlyPropertyDrawer : PropertyDrawer
    {
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
    /// Utility class for property reflection.
    /// </summary>
    public static class PropertyReflectionUtility
    {
        /// <summary>
        /// Gets a specific attribute from a SerializedProperty.
        /// </summary>
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

                    // Skip array elements, we need the array field info
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