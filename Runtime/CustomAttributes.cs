using System;
using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Allows customizing the display name of a field in the Inspector.
    /// </summary>
    /// <remarks>
    /// This attribute lets you specify a custom label for a serialized field
    /// in the Unity Inspector, overriding the default naming convention.
    /// This is useful for making the Inspector more user-friendly without
    /// changing variable names in code.
    /// 
    /// Example usage:
    /// <code>
    /// [Label("Player Speed")]
    /// [SerializeField] private float moveSpeed;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LabelAttribute : PropertyAttribute
    {
        /// <summary>
        /// The custom label to display in the Inspector.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Creates a new instance of the LabelAttribute with the specified label.
        /// </summary>
        /// <param name="label">The custom label to display in the Inspector.</param>
        public LabelAttribute(string label)
        {
            Label = label;
        }
    }

    /// <summary>
    /// Restricts the field to only accept assets from the project (not scene objects).
    /// </summary>
    /// <remarks>
    /// When applied to an Object reference field, this attribute restricts the field
    /// to only accept assets from the project (items from the Assets folder),
    /// preventing scene objects from being assigned. This is useful for ensuring
    /// that references point to persistent assets rather than scene-specific objects.
    /// 
    /// Example usage:
    /// <code>
    /// [AssetsOnly]
    /// public AudioClip soundEffect;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AssetsOnlyAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// Restricts the field to only accept objects from the current scene (not project assets).
    /// </summary>
    /// <remarks>
    /// When applied to an Object reference field, this attribute restricts the field
    /// to only accept objects from the current scene, preventing project assets from being
    /// assigned. This is useful for ensuring that references point to scene-specific objects
    /// rather than persistent assets.
    /// 
    /// Example usage:
    /// <code>
    /// [SceneObjectsOnly]
    /// public Transform target;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SceneObjectsOnlyAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// Groups fields into a visual box with a title.
    /// </summary>
    /// <remarks>
    /// This attribute organizes fields in the Inspector by grouping them within a titled box.
    /// All fields with the same group name will appear together within the same box.
    /// This improves organization of complex inspectors by visually separating related properties.
    /// 
    /// Example usage:
    /// <code>
    /// [GroupBox("Movement Settings")]
    /// public float moveSpeed;
    /// 
    /// [GroupBox("Movement Settings")]
    /// public float turnSpeed;
    /// 
    /// [GroupBox("Combat Settings")]
    /// public float attackDamage;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GroupBoxAttribute : PropertyAttribute
    {
        /// <summary>
        /// The name of the group that will appear as the box title.
        /// </summary>
        public string GroupName { get; private set; }

        /// <summary>
        /// Creates a new instance of the GroupBoxAttribute with the specified group name.
        /// </summary>
        /// <param name="groupName">The name of the group that will appear as the box title.</param>
        public GroupBoxAttribute(string groupName)
        {
            GroupName = groupName;
        }
    }

    /// <summary>
    /// Creates a collapsible foldout section for related fields.
    /// </summary>
    /// <remarks>
    /// This attribute creates a collapsible foldout in the Inspector for organizing related fields.
    /// All fields with the same foldout name will appear within the same foldout section.
    /// This is useful for hiding details that aren't frequently accessed, reducing clutter in the Inspector.
    /// 
    /// Foldouts can be nested within GroupBoxes to create hierarchical organization.
    /// 
    /// Example usage:
    /// <code>
    /// [FoldoutGroup("Advanced Settings")]
    /// public bool useCustomAnimation;
    /// 
    /// [FoldoutGroup("Advanced Settings")]
    /// public float animationSpeed;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class FoldoutGroupAttribute : PropertyAttribute
    {
        /// <summary>
        /// The name of the foldout section.
        /// </summary>
        public string FoldoutName { get; private set; }

        /// <summary>
        /// Creates a new instance of the FoldoutGroupAttribute with the specified foldout name.
        /// </summary>
        /// <param name="foldoutName">The name of the foldout section.</param>
        public FoldoutGroupAttribute(string foldoutName)
        {
            FoldoutName = foldoutName;
        }
    }

    /// <summary>
    /// Organizes fields into tabbed sections.
    /// </summary>
    /// <remarks>
    /// This attribute creates a tabbed interface in the Inspector for organizing fields
    /// into different categories. Fields with the same tab group name will appear in the
    /// same tabbed control, with individual tabs for each unique tab name.
    /// 
    /// This is particularly useful for components with many properties that can be
    /// categorized into distinct groups, allowing for a cleaner and more organized Inspector.
    /// 
    /// Example usage:
    /// <code>
    /// [TabGroup("PlayerSettings", "Movement")]
    /// public float moveSpeed;
    /// 
    /// [TabGroup("PlayerSettings", "Movement")]
    /// public float jumpHeight;
    /// 
    /// [TabGroup("PlayerSettings", "Combat")]
    /// public float attackDamage;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class TabGroupAttribute : PropertyAttribute
    {
        /// <summary>
        /// The name of the tab group container.
        /// </summary>
        public string TabGroupName { get; private set; }

        /// <summary>
        /// The name of the specific tab within the group.
        /// </summary>
        public string TabName { get; private set; }

        /// <summary>
        /// Creates a new instance of the TabGroupAttribute with the specified group and tab names.
        /// </summary>
        /// <param name="tabGroupName">The name of the tab group container.</param>
        /// <param name="tabName">The name of the specific tab within the group.</param>
        public TabGroupAttribute(string tabGroupName, string tabName)
        {
            TabGroupName = tabGroupName;
            TabName = tabName;
        }
    }
}