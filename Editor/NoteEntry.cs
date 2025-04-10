using System;

namespace ByteForge.Editor
{
    /// <summary>
    /// Represents a developer note attached to a specific component on a GameObject.
    /// </summary>
    /// <remarks>
    /// This serializable class stores information about where a note is placed in the scene hierarchy
    /// and the content of the note. It's designed to be saved and loaded as part of an
    /// editor utility for attaching notes to scene objects.
    /// </remarks>
    [Serializable]
    public class NoteEntry
    {
        /// <summary>
        /// The full hierarchical path to the GameObject.
        /// </summary>
        /// <remarks>
        /// Stores the scene hierarchy path in the format "/Parent/Child/Grandchild"
        /// to uniquely identify the GameObject the note is attached to.
        /// </remarks>
        public string GameObjectPath;
        
        /// <summary>
        /// The type name of the component the note is attached to.
        /// </summary>
        /// <remarks>
        /// When a GameObject has multiple components, this field identifies
        /// which specific component the note refers to.
        /// </remarks>
        public string ComponentType;
        
        /// <summary>
        /// The content of the note.
        /// </summary>
        /// <remarks>
        /// The actual text entered by the developer to document something
        /// about the GameObject or component.
        /// </remarks>
        public string Note;
        
        /// <summary>
        /// Generates a unique identifier for this note entry.
        /// </summary>
        /// <returns>A string combining the GameObject path and component type.</returns>
        /// <remarks>
        /// This key is used to uniquely identify and look up notes in a collection,
        /// ensuring that notes can be properly associated with their target components.
        /// </remarks>
        public string GetUniqueKey()
        {
            return $"{GameObjectPath}_{ComponentType}";
        }
    }
}