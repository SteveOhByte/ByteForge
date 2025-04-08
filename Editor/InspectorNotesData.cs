using System.Collections.Generic;
using UnityEngine;

namespace ByteForge.Editor
{
    [CreateAssetMenu(menuName = "Inspector Notes Data")]
    public class InspectorNotesData : ScriptableObject
    {
        public List<NoteEntry> Notes = new();
        public List<string> IgnoredTypes = new();
    }
}