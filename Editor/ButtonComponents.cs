#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Provides functionality for invoking methods from the Unity inspector via customizable buttons.
    /// </summary>
    /// <remarks>
    /// This class contains the core components used by the button system to enable method execution
    /// from the inspector. It includes classes for caching method information, storing parameter values,
    /// and drawing parameter fields in the inspector.
    /// </remarks>
    public class ButtonComponents
    {
        /// <summary>
        /// Caches information about methods that can be invoked from the inspector.
        /// </summary>
        /// <remarks>
        /// Stores method metadata along with parameter information and current parameter values.
        /// This cache improves performance by avoiding repeated reflection operations.
        /// </remarks>
        public class MethodCache
        {
            /// <summary>
            /// Gets the reflected method information.
            /// </summary>
            public MethodInfo Method { get; }
            
            /// <summary>
            /// Gets the display name to show on the button in the inspector.
            /// </summary>
            public string DisplayName { get; }
            
            /// <summary>
            /// Gets information about the method's parameters.
            /// </summary>
            public ParameterInfo[] Parameters { get; }
            
            /// <summary>
            /// Gets the current values for each parameter.
            /// </summary>
            public object[] ParameterValues { get; }

            /// <summary>
            /// Initializes a new instance of the MethodCache class.
            /// </summary>
            /// <param name="method">The reflected method information.</param>
            /// <param name="displayName">The name to show on the button in the inspector.</param>
            /// <remarks>
            /// Creates default values for each parameter based on the parameter type.
            /// Value types are initialized with Activator.CreateInstance, while reference types default to null.
            /// </remarks>
            public MethodCache(MethodInfo method, string displayName)
            {
                Method = method;
                DisplayName = displayName;
                Parameters = method.GetParameters();
                ParameterValues = new object[Parameters.Length];

                for (int i = 0; i < Parameters.Length; i++)
                {
                    ParameterValues[i] = Parameters[i].ParameterType.IsValueType
                        ? Activator.CreateInstance(Parameters[i].ParameterType)
                        : null;
                }
            }
        }

        /// <summary>
        /// Static cache to store methods for different component types.
        /// </summary>
        /// <remarks>
        /// Maintains a dictionary of component types to their button methods,
        /// avoiding the need to use reflection to find the methods each time
        /// the inspector is drawn.
        /// </remarks>
        public static class ButtonMethodCache
        {
            /// <summary>
            /// Dictionary mapping component types to their button methods.
            /// </summary>
            private static readonly Dictionary<Type, List<MethodCache>> cachedMethods = new();

            /// <summary>
            /// Gets or creates a list of button methods for a specific component type.
            /// </summary>
            /// <param name="type">The component type to get methods for.</param>
            /// <returns>A list of MethodCache objects for the specified type.</returns>
            /// <remarks>
            /// If the methods for this type are already cached, returns them from the cache.
            /// Otherwise, uses reflection to find all methods with the InspectorButtonAttribute
            /// and caches them for future use.
            /// </remarks>
            public static List<MethodCache> GetMethodsForType(Type type)
            {
                if (cachedMethods.TryGetValue(type, out List<MethodCache> methods))
                    return methods;

                List<MethodCache> methodList = new();
                MethodInfo[] typeMethods =
                    type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (MethodInfo method in typeMethods)
                {
                    InspectorButtonAttribute attribute = method.GetCustomAttribute<InspectorButtonAttribute>();
                    if (attribute != null)
                    {
                        string displayName = string.IsNullOrEmpty(attribute.ButtonName)
                            ? ObjectNames.NicifyVariableName(method.Name)
                            : attribute.ButtonName;

                        methodList.Add(new(method, displayName));
                    }
                }

                cachedMethods[type] = methodList;
                return methodList;
            }
        }

        /// <summary>
        /// Utility for drawing parameter fields in the inspector.
        /// </summary>
        /// <remarks>
        /// Provides methods to draw appropriate editor fields for different parameter types,
        /// supporting a wide range of common Unity and C# types.
        /// </remarks>
        public static class ParameterDrawer
        {
            /// <summary>
            /// Draws an appropriate inspector field for a method parameter.
            /// </summary>
            /// <param name="parameter">Information about the parameter.</param>
            /// <param name="currentValue">The current value of the parameter.</param>
            /// <returns>The new value from the inspector field.</returns>
            /// <remarks>
            /// Supports common types like int, float, bool, string, Vector2/3, Color, enums,
            /// Unity Objects, and types with static readonly fields (like constants).
            /// For unsupported types, displays a disabled text field with a message.
            /// </remarks>
            public static object DrawParameterField(ParameterInfo parameter, object currentValue)
            {
                Type parameterType = parameter.ParameterType;
                string parameterName = ObjectNames.NicifyVariableName(parameter.Name);

                if (parameterType == typeof(int))
                    return EditorGUILayout.IntField(parameterName, (int)currentValue);
                if (parameterType == typeof(float))
                    return EditorGUILayout.FloatField(parameterName, (float)currentValue);
                if (parameterType == typeof(bool))
                    return EditorGUILayout.Toggle(parameterName, (bool)currentValue);
                if (parameterType == typeof(string))
                    return EditorGUILayout.TextField(parameterName, (string)currentValue);
                if (parameterType == typeof(Vector2))
                    return EditorGUILayout.Vector2Field(parameterName, (Vector2)currentValue);
                if (parameterType == typeof(Vector3))
                    return EditorGUILayout.Vector3Field(parameterName, (Vector3)currentValue);
                if (parameterType == typeof(Color))
                    return EditorGUILayout.ColorField(parameterName, (Color)currentValue);
                if (parameterType.IsEnum)
                    return EditorGUILayout.EnumPopup(parameterName, (Enum)currentValue);
                if (HasStaticReadOnlyFields(parameterType))
                    return HandleStaticReadOnlyFieldsType(parameterName, parameterType, currentValue);
                if (typeof(UnityEngine.Object).IsAssignableFrom(parameterType))
                    return EditorGUILayout.ObjectField(parameterName, (UnityEngine.Object)currentValue, parameterType,
                        true);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(parameterName, "Unsupported Type: " + parameterType.Name);
                EditorGUI.EndDisabledGroup();
                return currentValue;
            }

            /// <summary>
            /// Determines if a type has static readonly fields that can be used as constants.
            /// </summary>
            /// <param name="type">The type to check.</param>
            /// <returns>True if the type has static readonly fields of its own type; otherwise, false.</returns>
            /// <remarks>
            /// This is used to support types that define constants as static readonly fields,
            /// allowing them to be displayed as dropdown menus in the inspector.
            /// </remarks>
            private static bool HasStaticReadOnlyFields(Type type)
            {
                return type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Any(f => f.IsInitOnly && f.FieldType == type);
            }

            /// <summary>
            /// Draws a dropdown for types with static readonly fields.
            /// </summary>
            /// <param name="parameterName">The name of the parameter.</param>
            /// <param name="type">The type with static readonly fields.</param>
            /// <param name="currentValue">The current value of the parameter.</param>
            /// <returns>The selected static field value.</returns>
            /// <remarks>
            /// Creates a popup menu containing all static readonly fields of the specified type,
            /// allowing them to be selected as parameter values.
            /// </remarks>
            private static object HandleStaticReadOnlyFieldsType(string parameterName, Type type, object currentValue)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                FieldInfo[] validFields = fields.Where(f => f.IsInitOnly && f.FieldType == type).ToArray();

                object[] values = validFields.Select(f => f.GetValue(null)).ToArray();
                string[] names = validFields.Select(f => f.Name).ToArray();

                int currentIndex = Array.IndexOf(values, currentValue);
                if (currentIndex < 0) currentIndex = 0;

                int selectedIndex = EditorGUILayout.Popup(parameterName, currentIndex, names);
                return values[selectedIndex];
            }
        }
    }
}
#endif