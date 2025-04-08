#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using ByteForge.Runtime;

namespace ByteForge.Editor
{
    [CustomPropertyDrawer(typeof(ShowIf))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
                return EditorGUI.GetPropertyHeight(property, label);
            return 0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
                EditorGUI.PropertyField(position, property, label);
        }

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

        private int CompareValues(object value1, object value2)
        {
            if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
                return comparable1.CompareTo(comparable2);
            return 0;
        }
    }
}
#endif