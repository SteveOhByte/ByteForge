using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// A custom property attribute to mark fields in the inspector as "required."
    /// This attribute can be paired with a custom property drawer to visually indicate
    /// fields in the Unity editor that must be populated.
    /// </summary>
    public class Required : PropertyAttribute { }
}