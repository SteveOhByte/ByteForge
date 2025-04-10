using UnityEngine;
using System;
using System.Collections.Generic;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Defines the types of comparisons that can be made between fields and values.
    /// </summary>
    /// <remarks>
    /// These comparison types are used in conditional attributes to determine
    /// when fields should be shown or hidden in the inspector.
    /// </remarks>
    public enum ComparisonType
    {
        /// <summary>Field value must be equal to the comparison value.</summary>
        EQUAL_TO,
        
        /// <summary>Field value must not be equal to the comparison value.</summary>
        NOT_EQUAL_TO,
        
        /// <summary>Field value must be greater than the comparison value.</summary>
        GREATER_THAN,
        
        /// <summary>Field value must be less than the comparison value.</summary>
        LESS_THAN,
        
        /// <summary>Field value must be greater than or equal to the comparison value.</summary>
        GREATER_THAN_OR_EQUAL_TO,
        
        /// <summary>Field value must be less than or equal to the comparison value.</summary>
        LESS_THAN_OR_EQUAL_TO
    }

    /// <summary>
    /// Defines the logical operators that can be used to combine multiple conditions.
    /// </summary>
    public enum LogicalOperator
    {
        /// <summary>All conditions must be true (logical AND).</summary>
        AND,
        
        /// <summary>At least one condition must be true (logical OR).</summary>
        OR
    }

    /// <summary>
    /// Represents a single condition for conditional attributes.
    /// </summary>
    /// <remarks>
    /// A condition consists of a field name, a comparison type, a value to compare against,
    /// and optionally a logical operator to combine with other conditions.
    /// </remarks>
    [Serializable]
    public struct Condition
    {
        /// <summary>
        /// The name of the field to check.
        /// </summary>
        /// <remarks>
        /// This should be the exact name of a field in the same class as the conditional field,
        /// including proper capitalization.
        /// </remarks>
        public readonly string FieldName;
        
        /// <summary>
        /// The type of comparison to perform.
        /// </summary>
        public readonly ComparisonType ComparisonType;
        
        /// <summary>
        /// The value to compare against.
        /// </summary>
        /// <remarks>
        /// This value should be of a type that can be compared with the field value.
        /// For enums, use the actual enum value, not the string representation.
        /// </remarks>
        public readonly object CompareValue;
        
        /// <summary>
        /// The logical operator to use when combining with other conditions.
        /// </summary>
        /// <remarks>
        /// This is only relevant when multiple conditions are specified.
        /// </remarks>
        public readonly LogicalOperator Operator;

        /// <summary>
        /// Creates a new condition.
        /// </summary>
        /// <param name="fieldName">The name of the field to check.</param>
        /// <param name="comparisonType">The type of comparison to perform.</param>
        /// <param name="compareValue">The value to compare against.</param>
        /// <param name="op">The logical operator to use when combining with other conditions. Defaults to AND.</param>
        public Condition(string fieldName, ComparisonType comparisonType, object compareValue,
            LogicalOperator op = LogicalOperator.AND)
        {
            FieldName = fieldName;
            ComparisonType = comparisonType;
            CompareValue = compareValue;
            Operator = op;
        }
    }

    /// <summary>
    /// Attribute that conditionally shows or hides fields in the inspector based on the values of other fields.
    /// </summary>
    /// <remarks>
    /// This attribute allows you to create dynamic inspectors where fields appear or disappear
    /// based on the values of other fields. This helps reduce clutter and only show relevant fields.
    /// 
    /// The ShowIf attribute uses reflection to access the values of other fields in the class
    /// and compares them using the specified comparison types and logical operators.
    /// 
    /// Example usage:
    /// <code>
    /// // Basic usage - shows attackDamage only when isAttacker is true
    /// [SerializeField] private bool isAttacker;
    /// 
    /// [ShowIf("isAttacker")]
    /// [SerializeField] private float attackDamage;
    /// 
    /// // Advanced usage - shows healAmount only when characterClass equals "Healer"
    /// [SerializeField] private string characterClass;
    /// 
    /// [ShowIf("characterClass", ComparisonType.EQUAL_TO, "Healer")]
    /// [SerializeField] private float healAmount;
    /// 
    /// // Complex usage - shows speedBoost when characterClass is "Rogue" OR level is greater than 10
    /// [SerializeField] private int level;
    /// 
    /// [ShowIf("characterClass", ComparisonType.EQUAL_TO, "Rogue", LogicalOperator.OR,
    ///          "level", ComparisonType.GREATER_THAN, 10)]
    /// [SerializeField] private float speedBoost;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowIf : PropertyAttribute
    {
        /// <summary>
        /// The conditions that determine whether the field should be shown.
        /// </summary>
        public readonly Condition[] Conditions;

        /// <summary>
        /// Creates a ShowIf attribute that shows the field when the specified boolean field is true.
        /// </summary>
        /// <param name="conditionField">The name of the boolean field to check.</param>
        /// <remarks>
        /// This is a convenience constructor for the common case of showing a field
        /// based on a boolean toggle.
        /// </remarks>
        public ShowIf(string conditionField)
        {
            Conditions = new[] { new Condition(conditionField, ComparisonType.EQUAL_TO, true) };
        }

        /// <summary>
        /// Creates a ShowIf attribute with a single condition.
        /// </summary>
        /// <param name="conditionField">The name of the field to check.</param>
        /// <param name="comparisonType">The type of comparison to perform.</param>
        /// <param name="compareValue">The value to compare against.</param>
        /// <remarks>
        /// This constructor allows for more complex conditions with different comparison types
        /// and values other than just booleans.
        /// </remarks>
        public ShowIf(string conditionField, ComparisonType comparisonType, object compareValue)
        {
            Conditions = new[] { new Condition(conditionField, comparisonType, compareValue) };
        }

        /// <summary>
        /// Creates a ShowIf attribute with multiple conditions.
        /// </summary>
        /// <param name="conditions">
        /// An array of objects representing the conditions, where each condition consists of:
        /// - string: Field name
        /// - ComparisonType: Type of comparison
        /// - object: Value to compare against
        /// - LogicalOperator: Operator to combine with the next condition (optional for the last condition)
        /// </param>
        /// <remarks>
        /// This constructor allows for complex conditional logic with multiple conditions
        /// combined using AND/OR operators.
        /// 
        /// The parameters should be provided in groups of 3 or 4:
        /// - For the first N-1 conditions: fieldName, comparisonType, compareValue, logicalOperator
        /// - For the last condition: fieldName, comparisonType, compareValue
        /// 
        /// Example:
        /// <code>
        /// [ShowIf("health", ComparisonType.LESS_THAN, 50, LogicalOperator.AND,
        ///          "hasHealingItem", ComparisonType.EQUAL_TO, true)]
        /// </code>
        /// </remarks>
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