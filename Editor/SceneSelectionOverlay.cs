#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ByteForge.Editor
{
    [Overlay(typeof(SceneView), "Scene Selection")]
    [Icon(kIcon)]
    public class SceneSelectionOverlay : ToolbarOverlay
    {
        private const string kIcon = "Assets/ByteForge/Icons/UnityIcon.png";

        private SceneSelectionOverlay() : base(SceneDropdownToggle.kId)
        {
        }

        [EditorToolbarElement(kId, typeof(SceneView))]
        internal class SceneDropdownToggle : EditorToolbarDropdownToggle, IAccessContainerWindow
        {
            public const string kId = "SceneSelectionOverlay/SceneDropdownToggle";

            public EditorWindow containerWindow { get; set; }

            private SceneDropdownToggle()
            {
                text = "Scenes";
                tooltip = "Select a scene to load";
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(kIcon);

                dropdownClicked += ShowSceneMenu;
            }

            private void ShowSceneMenu()
            {
                GenericMenu menu = new();

                Scene currentScene = EditorSceneManager.GetActiveScene();

                string[] sceneGuids = AssetDatabase.FindAssets("t:scene", null);

                foreach (string guid in sceneGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    string name = Path.GetFileNameWithoutExtension(path);

                    menu.AddItem(new(name), string.Compare(currentScene.name, name) == 0,
                        () => OpenScene(currentScene, path));
                }

                menu.ShowAsContext();
            }

            private void OpenScene(Scene currentScene, string path)
            {
                if (currentScene.isDirty)
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(path);
                }
                else
                {
                    EditorSceneManager.OpenScene(path);
                }
            }
        }
    }
}
#endif