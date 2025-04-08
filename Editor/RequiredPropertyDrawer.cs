#if UNITY_EDITOR // Ensure this code is only compiled and used in the editor, not in builds
using ByteForge.Runtime;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Custom drawer for the 'Required' attribute.
    /// This marks any object reference field as required when it is null,
    /// and displays an error message in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(Required))]
    public class RequiredPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Returns the height of the property in the Unity inspector
        /// Adjusts height if the property is an object reference and it's null
        /// </summary>
        /// <param name="property">The serialized property being drawn</param>
        /// <param name="label">The label of the property</param>
        /// <returns>The height of the property in pixels</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                property.objectReferenceValue == null)
            {
                // If the property is an object reference and it's null, adjust height to display the error message.
                return base.GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight + 4f;
            }

            return base.GetPropertyHeight(property, label);
        }

        /// <summary>
        /// Draws the GUI in the Unity inspector for the 'Required' attribute
        /// If the property is an object reference and it's null, display an error message
        /// </summary>
        /// <param name="position">The position to draw the GUI</param>
        /// <param name="property">The serialized property being drawn</param>
        /// <param name="label">The label of the property</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Adjust the position for the property field
            Rect propertyPosition = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(propertyPosition, property, label);

            if (property.propertyType != SerializedPropertyType.ObjectReference ||
                property.objectReferenceValue != null) return;

            // Display an error help box if the property is an object reference and it's null.
            Color previousColour = GUI.backgroundColor; // Store the previous background color
            GUI.backgroundColor = Color.red; // Set the background color to red for the error message

            Rect helpBoxPosition = new(position.x, position.y + EditorGUIUtility.singleLineHeight + 2f, position.width,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(helpBoxPosition, ObjectNames.NicifyVariableName(property.name) + " is required!",
                MessageType.Error);

            GUI.backgroundColor = previousColour; // Restore the previous background color
        }
    }
}
#endif