using System;

namespace ByteForge.Editor
{
    [Serializable]
    public class NoteEntry
    {
        public string GameObjectPath;
        public string ComponentType;
        public string Note;
        
        public string GetUniqueKey()
        {
            return $"{GameObjectPath}_{ComponentType}";
        }
    }
}