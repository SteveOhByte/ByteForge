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
    /// <summary>
    /// Creates an overlay in the Scene view for quickly selecting and switching between scenes.
    /// </summary>
    /// <remarks>
    /// This overlay adds a dropdown button to the Unity Scene view that lists all scenes in the project,
    /// allowing developers to quickly switch between them without using the Project window.
    /// </remarks>
    [Overlay(typeof(SceneView), "Scene Selection")]
    [Icon(kIcon)]
    public class SceneSelectionOverlay : ToolbarOverlay
    {
        /// <summary>
        /// The path to the icon used for the scene selection overlay.
        /// </summary>
        private const string kIcon = "Assets/ByteForge/Icons/UnityIcon.png";

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneSelectionOverlay"/> class.
        /// </summary>
        /// <remarks>
        /// Constructs the overlay with the scene dropdown toggle element.
        /// </remarks>
        private SceneSelectionOverlay() : base(SceneDropdownToggle.kId)
        {
        }

        /// <summary>
        /// A dropdown toggle that displays a list of scenes in the project.
        /// </summary>
        /// <remarks>
        /// This internal class handles the dropdown button UI and the logic for scene switching.
        /// </remarks>
        [EditorToolbarElement(kId, typeof(SceneView))]
        internal class SceneDropdownToggle : EditorToolbarDropdownToggle, IAccessContainerWindow
        {
            /// <summary>
            /// The identifier for the scene dropdown toggle element.
            /// </summary>
            public const string kId = "SceneSelectionOverlay/SceneDropdownToggle";

            /// <summary>
            /// Gets or sets the container window for this dropdown toggle.
            /// </summary>
            /// <remarks>
            /// Required by the IAccessContainerWindow interface to access the parent window.
            /// </remarks>
            public EditorWindow containerWindow { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="SceneDropdownToggle"/> class.
            /// </summary>
            /// <remarks>
            /// Sets up the button appearance and registers the click handler for the dropdown menu.
            /// </remarks>
            private SceneDropdownToggle()
            {
                text = "Scenes";
                tooltip = "Select a scene to load";
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(kIcon);

                dropdownClicked += ShowSceneMenu;
            }

            /// <summary>
            /// Displays a context menu with a list of all scenes in the project.
            /// </summary>
            /// <remarks>
            /// When called, this method finds all scene assets in the project and lists them
            /// in a context menu, highlighting the currently active scene.
            /// </remarks>
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

            /// <summary>
            /// Opens the selected scene, handling unsaved changes in the current scene.
            /// </summary>
            /// <param name="currentScene">The currently active scene.</param>
            /// <param name="path">The path to the scene to open.</param>
            /// <remarks>
            /// If the current scene has unsaved changes, prompts the user to save before
            /// switching to the new scene. Otherwise, opens the new scene directly.
            /// </remarks>
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