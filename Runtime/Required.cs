using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// A custom property attribute to mark fields in the inspector as "required."
    /// </summary>
    /// <remarks>
    /// This attribute can be paired with a custom property drawer to visually indicate
    /// fields in the Unity editor that must be populated. When applied to a field,
    /// the RequiredPropertyDrawer will display an error message in the inspector
    /// if the field is null or empty.
    /// 
    /// This helps prevent common issues where required references are not set,
    /// catching configuration errors early in the development process rather than
    /// encountering NullReferenceExceptions at runtime.
    /// 
    /// Example usage:
    /// <code>
    /// [Required]
    /// [SerializeField] private Transform targetTransform;
    /// 
    /// [Required]
    /// public AudioClip impactSound;
    /// </code>
    /// 
    /// The PlayModeStateChecker in the Editor namespace also works with this attribute
    /// to prevent entering play mode if any Required fields are not assigned.
    /// </remarks>
    public class Required : PropertyAttribute { }
}