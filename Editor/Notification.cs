#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    public static class Notification
    {
        public static void Display(string text)
        {
            GUIContent content = new(text);
            SceneView.lastActiveSceneView?.ShowNotification(content, 2);
        }
    }
}
#endif