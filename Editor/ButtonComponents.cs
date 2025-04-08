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
    public class ButtonComponents
    {
        /// <summary>
        /// Caches information about methods that can be invoked from the inspector.
        /// </summary>
        public class MethodCache
        {
            public MethodInfo Method { get; }
            public string DisplayName { get; }
            public ParameterInfo[] Parameters { get; }
            public object[] ParameterValues { get; }

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
        public static class ButtonMethodCache
        {
            private static readonly Dictionary<Type, List<MethodCache>> cachedMethods = new();

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
        /// Utility for drawing parameter fields for method parameters.
        /// </summary>
        public static class ParameterDrawer
        {
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

            private static bool HasStaticReadOnlyFields(Type type)
            {
                return type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Any(f => f.IsInitOnly && f.FieldType == type);
            }

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