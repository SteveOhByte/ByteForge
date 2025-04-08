using UnityEngine;
using System;
using System.Collections.Generic;

namespace ByteForge.Runtime
{
    public enum ComparisonType
    {
        EQUAL_TO,
        NOT_EQUAL_TO,
        GREATER_THAN,
        LESS_THAN,
        GREATER_THAN_OR_EQUAL_TO,
        LESS_THAN_OR_EQUAL_TO
    }

    public enum LogicalOperator
    {
        AND,
        OR
    }

    [Serializable]
    public struct Condition
    {
        public readonly string FieldName;
        public readonly ComparisonType ComparisonType;
        public readonly object CompareValue;
        public readonly LogicalOperator Operator;

        public Condition(string fieldName, ComparisonType comparisonType, object compareValue,
            LogicalOperator op = LogicalOperator.AND)
        {
            FieldName = fieldName;
            ComparisonType = comparisonType;
            CompareValue = compareValue;
            Operator = op;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ShowIf : PropertyAttribute
    {
        public readonly Condition[] Conditions;

        // Simple boolean check
        public ShowIf(string conditionField)
        {
            Conditions = new[] { new Condition(conditionField, ComparisonType.EQUAL_TO, true) };
        }

        // Single condition with comparison
        public ShowIf(string conditionField, ComparisonType comparisonType, object compareValue)
        {
            Conditions = new[] { new Condition(conditionField, comparisonType, compareValue) };
        }

        // Multiple conditions
        public ShowIf(params object[] conditions)
        {
            List<Condition> conditionsList = new();

            for (int i = 0; i < conditions.Length; i += 4)
            {
                string fieldName = (string)conditions[i];
                ComparisonType compType = (ComparisonType)conditions[i + 1];
                object compareValue = conditions[i + 2];
                LogicalOperator op = i + 3 < conditions.Length
                    ? (LogicalOperator)conditions[i + 3]
                    : LogicalOperator.AND;

                conditionsList.Add(new(fieldName, compType, compareValue, op));
            }

            Conditions = conditionsList.ToArray();
        }
    }
}