using System.Collections.Generic;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// ScriptableObject that stores developer notes and configuration for the Inspector Notes system.
    /// </summary>
    /// <remarks>
    /// This asset stores a collection of notes attached to components throughout the project,
    /// allowing developers to add explanatory text to objects in the scene. It also maintains
    /// a list of component types that should be ignored by the notes system.
    /// </remarks>
    [CreateAssetMenu(menuName = "Inspector Notes Data")]
    public class InspectorNotesData : ScriptableObject
    {
        /// <summary>
        /// Collection of developer notes attached to components.
        /// </summary>
        /// <remarks>
        /// Each entry contains information about the location of the note (GameObject path and component type)
        /// and the content of the note itself. This list is serialized and saved with the asset.
        /// </remarks>
        public List<NoteEntry> Notes = new();
        
        /// <summary>
        /// List of component type names to exclude from the notes system.
        /// </summary>
        /// <remarks>
        /// These component types will not show the notes interface in the inspector.
        /// This can be useful for built-in Unity components or third-party components
        /// that you don't want to add notes to.
        /// </remarks>
        public List<string> IgnoredTypes = new();
    }
}