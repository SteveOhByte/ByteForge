#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    /// <summary>
    /// Provides editor functionality to quickly reorder components on GameObjects.
    /// </summary>
    /// <remarks>
    /// This utility adds context menu items to all Components in the Inspector,
    /// allowing developers to instantly move a component to the top or bottom
    /// of the component list, rather than repeatedly clicking the up/down buttons.
    /// </remarks>
    public static class MoveComponentTool
    {
        /// <summary>
        /// The menu item path for the "Move To Top" context menu option.
        /// </summary>
        private const string kMenuMoveToTop = "CONTEXT/Component/Move To Top";
        
        /// <summary>
        /// The menu item path for the "Move To Bottom" context menu option.
        /// </summary>
        private const string kMenuMoveToBottom = "CONTEXT/Component/Move To Bottom";

        /// <summary>
        /// Moves the selected component to the top of the component list.
        /// </summary>
        /// <param name="command">The MenuCommand containing the target component.</param>
        /// <remarks>
        /// Uses Unity's ComponentUtility to repeatedly move the component up
        /// until it cannot be moved any further (reaches the top).
        /// </remarks>
        [MenuItem(kMenuMoveToTop, priority = 501)]
        public static void MoveComponentToTopMenuItem(MenuCommand command)
        {
            while (UnityEditorInternal.ComponentUtility.MoveComponentUp((Component)command.context)) ;
        }

        /// <summary>
        /// Validates whether the "Move To Top" menu item should be enabled.
        /// </summary>
        /// <param name="command">The MenuCommand containing the target component.</param>
        /// <returns>
        /// False if the component is already at the top position (or the Transform component);
        /// otherwise, true.
        /// </returns>
        /// <remarks>
        /// This method is called by Unity to determine if the "Move To Top" menu item
        /// should be enabled or disabled in the context menu.
        /// </remarks>
        [MenuItem(kMenuMoveToTop, validate = true)]
        public static bool MoveComponentToTopMenuItemValidate(MenuCommand command)
        {
            Component[] components = ((Component)command.context).gameObject.GetComponents<Component>();

            return !components.Where((t, i) => t == ((Component)command.context) && i == 1).Any();
        }

        /// <summary>
        /// Validates whether the "Move To Bottom" menu item should be enabled.
        /// </summary>
        /// <param name="command">The MenuCommand containing the target component.</param>
        /// <returns>
        /// False if the component is already at the bottom position;
        /// otherwise, true.
        /// </returns>
        /// <remarks>
        /// This method is called by Unity to determine if the "Move To Bottom" menu item
        /// should be enabled or disabled in the context menu.
        /// </remarks>
        [MenuItem(kMenuMoveToBottom, validate = true)]
        public static bool MoveComponentToBottomMenuItemValidate(MenuCommand command)
        {
            Component[] components = ((Component)command.context).gameObject.GetComponents<Component>();

            return !components.Where((t, i) => t == ((Component)command.context) && i == components.Length - 1).Any();
        }

        /// <summary>
        /// Moves the selected component to the bottom of the component list.
        /// </summary>
        /// <param name="command">The MenuCommand containing the target component.</param>
        /// <remarks>
        /// Uses Unity's ComponentUtility to repeatedly move the component down
        /// until it cannot be moved any further (reaches the bottom).
        /// </remarks>
        [MenuItem(kMenuMoveToBottom, priority = 501)]
        public static void MoveComponentToBottomMenuItem(MenuCommand command)
        {
            while (UnityEditorInternal.ComponentUtility.MoveComponentDown((Component)command.context)) ;
        }
    }
}
#endif