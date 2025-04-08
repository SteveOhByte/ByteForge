using System;
using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Allows customizing the display name of a field in the Inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LabelAttribute : PropertyAttribute
    {
        public string Label { get; private set; }

        public LabelAttribute(string label)
        {
            Label = label;
        }
    }

    /// <summary>
    /// Restricts the field to only accept assets from the project (not scene objects).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AssetsOnlyAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// Restricts the field to only accept objects from the current scene (not project assets).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SceneObjectsOnlyAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// Groups fields into a visual box with a title.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GroupBoxAttribute : PropertyAttribute
    {
        public string GroupName { get; private set; }

        public GroupBoxAttribute(string groupName)
        {
            GroupName = groupName;
        }
    }

    /// <summary>
    /// Creates a collapsible foldout section for related fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class FoldoutGroupAttribute : PropertyAttribute
    {
        public string FoldoutName { get; private set; }

        public FoldoutGroupAttribute(string foldoutName)
        {
            FoldoutName = foldoutName;
        }
    }

    /// <summary>
    /// Organizes fields into tabbed sections.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class TabGroupAttribute : PropertyAttribute
    {
        public string TabGroupName { get; private set; }
        public string TabName { get; private set; }

        public TabGroupAttribute(string tabGroupName, string tabName)
        {
            TabGroupName = tabGroupName;
            TabName = tabName;
        }
    }
}