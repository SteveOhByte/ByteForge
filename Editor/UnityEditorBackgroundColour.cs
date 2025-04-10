#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Provides background colour utilities for Unity Editor UI elements.
    /// </summary>
    /// <remarks>
    /// Contains predefined colours for various editor states including normal, selected, 
    /// unfocused, and hovered states, with variants for both light and dark (Pro) skin.
    /// </remarks>
    public static class UnityEditorBackgroundColour
    {
        /// <summary>
        /// Default background colour for light skin.
        /// </summary>
        private static readonly Color kDefaultColour = new(0.7843f, 0.7843f, 0.7843f);

        /// <summary>
        /// Default background colour for dark (Pro) skin.
        /// </summary>
        private static readonly Color kDefaultProColour = new(0.2196f, 0.2196f, 0.2196f);

        /// <summary>
        /// Selected item background colour for light skin.
        /// </summary>
        private static readonly Color kSelectedColour = new(0.22745f, 0.447f, 0.6902f);

        /// <summary>
        /// Selected item background colour for dark (Pro) skin.
        /// </summary>
        private static readonly Color kSelectedProColour = new(0.1725f, 0.3647f, 0.5294f);

        /// <summary>
        /// Selected but unfocused item background colour for light skin.
        /// </summary>
        private static readonly Color kSelectedUnFocusedColour = new(0.68f, 0.68f, 0.68f);

        /// <summary>
        /// Selected but unfocused item background colour for dark (Pro) skin.
        /// </summary>
        private static readonly Color kSelectedUnFocusedProColour = new(0.3f, 0.3f, 0.3f);

        /// <summary>
        /// Hovered item background colour for light skin.
        /// </summary>
        private static readonly Color kHoveredColour = new(0.698f, 0.698f, 0.698f);

        /// <summary>
        /// Hovered item background colour for dark (Pro) skin.
        /// </summary>
        private static readonly Color kHoveredProColour = new(0.2706f, 0.2706f, 0.2706f);

        /// <summary>
        /// Gets the appropriate background colour based on the current item state and editor skin.
        /// </summary>
        /// <param name="isSelected">Whether the item is selected.</param>
        /// <param name="isHovered">Whether the item is being hovered over.</param>
        /// <param name="isWindowFocused">Whether the containing window has focus.</param>
        /// <returns>The appropriate Color for the given state.</returns>
        public static Color Get(bool isSelected, bool isHovered, bool isWindowFocused)
        {
            if (isSelected)
            {
                if (isWindowFocused)
                    return EditorGUIUtility.isProSkin ? kSelectedProColour : kSelectedColour;
                return EditorGUIUtility.isProSkin ? kSelectedUnFocusedProColour : kSelectedUnFocusedColour;
            }

            if (isHovered)
                return EditorGUIUtility.isProSkin ? kHoveredProColour : kHoveredColour;
            return EditorGUIUtility.isProSkin ? kDefaultProColour : kDefaultColour;
        }
    }
}
#endif