#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    public static class UnityEditorBackgroundColour
    {
        private static readonly Color kDefaultColour = new(0.7843f, 0.7843f, 0.7843f);
        private static readonly Color kDefaultProColour = new(0.2196f, 0.2196f, 0.2196f);

        private static readonly Color kSelectedColour = new(0.22745f, 0.447f, 0.6902f);
        private static readonly Color kSelectedProColour = new(0.1725f, 0.3647f, 0.5294f);

        private static readonly Color kSelectedUnFocusedColour = new(0.68f, 0.68f, 0.68f);
        private static readonly Color kSelectedUnFocusedProColour = new(0.3f, 0.3f, 0.3f);

        private static readonly Color kHoveredColour = new(0.698f, 0.698f, 0.698f);
        private static readonly Color kHoveredProColour = new(0.2706f, 0.2706f, 0.2706f);

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