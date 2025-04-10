#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using ByteForge.Runtime;

namespace ByteForge.Editor
{
    /// <summary>
    /// Custom property drawer for the 'ShowIf' attribute.
    /// </summary>
    /// <remarks>
    /// This drawer conditionally shows or hides properties in the inspector
    /// based on specified conditions that evaluate field values on the target object.
    /// </remarks>
    [CustomPropertyDrawer(typeof(ShowIf))]
    public class ShowIfDrawer : PropertyDrawer
    {
        /// <summary>
        /// Returns the height of the property in the Unity inspector.
        /// </summary>
        /// <param name="property">The serialized property being drawn.</param>
        /// <param name="label">The label of the property.</param>
        /// <returns>
        /// The height of the property in pixels if the condition evaluates to true;
        /// otherwise, returns 0 to hide the property.
        /// </returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
                return EditorGUI.GetPropertyHeight(property, label);
            return 0f;
        }

        /// <summary>
        /// Draws the GUI in the Unity inspector for the property with the 'ShowIf' attribute.
        /// </summary>
        /// <param name="position">The position to draw the GUI.</param>
        /// <param name="property">The serialized property being drawn.</param>
        /// <param name="label">The label of the property.</param>
        /// <remarks>
        /// Only draws the property if the condition evaluates to true; otherwise, the property is hidden.
        /// </remarks>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
                EditorGUI.PropertyField(position, property, label);
        }

        /// <summary>
        /// Determines whether the property should be shown based on the conditions.
        /// </summary>
        /// <param name="property">The serialized property being evaluated.</param>
        /// <returns>True if the property should be shown; otherwise, false.</returns>
        /// <remarks>
        /// Evaluates all conditions in the ShowIf attribute and combines them according to 
        /// their logical operators (AND/OR) to determine visibility.
        /// </remarks>
        private bool ShouldShow(SerializedProperty property)
        {
            ShowIf showIfAttribute = (ShowIf)attribute;
            object target = property.serializedObject.targetObject;

            bool result = EvaluateCondition(showIfAttribute.Conditions[0], target);

            for (int i = 1; i < showIfAttribute.Conditions.Length; i++)
            {
                Condition condition = showIfAttribute.Conditions[i];
                bool nextResult = EvaluateCondition(condition, target);

                if (showIfAttribute.Conditions[i - 1].Operator == LogicalOperator.AND)
                    result &= nextResult;
                else
                    result |= nextResult;
            }

            return result;
        }

        /// <summary>
        /// Evaluates a single condition on the target object.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="target">The target object containing the field to check.</param>
        /// <returns>True if the condition is satisfied; otherwise, false.</returns>
        /// <remarks>
        /// Uses reflection to access the field value on the target object and compares it
        /// to the condition's comparison value using the specified comparison type.
        /// </remarks>
        private bool EvaluateCondition(Condition condition, object target)
        {
            FieldInfo conditionField = target.GetType().GetField(condition.FieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (conditionField == null)
            {
                //DarkDebug.LogError($"Condition field '{condition.FieldName}' not found on {target.GetType().Name}");
                return true;
            }

            object currentValue = conditionField.GetValue(target);

            return condition.ComparisonType switch
            {
                ComparisonType.EQUAL_TO => Equals(currentValue, condition.CompareValue),
                ComparisonType.NOT_EQUAL_TO => !Equals(currentValue, condition.CompareValue),
                ComparisonType.GREATER_THAN => CompareValues(currentValue, condition.CompareValue) > 0,
                ComparisonType.LESS_THAN => CompareValues(currentValue, condition.CompareValue) < 0,
                ComparisonType.GREATER_THAN_OR_EQUAL_TO => CompareValues(currentValue, condition.CompareValue) >= 0,
                ComparisonType.LESS_THAN_OR_EQUAL_TO => CompareValues(currentValue, condition.CompareValue) <= 0,
                _ => true
            };
        }

        /// <summary>
        /// Compares two values using the IComparable interface.
        /// </summary>
        /// <param name="value1">The first value to compare.</param>
        /// <param name="value2">The second value to compare.</param>
        /// <returns>
        /// A negative integer if value1 is less than value2, zero if they are equal,
        /// or a positive integer if value1 is greater than value2.
        /// </returns>
        /// <remarks>
        /// If the values do not implement IComparable, returns 0 (equal).
        /// </remarks>
        private int CompareValues(object value1, object value2)
        {
            if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
                return comparable1.CompareTo(comparable2);
            return 0;
        }
    }
}
#endif