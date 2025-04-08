#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ByteForge.Editor
{
    public static class MoveComponentTool
    {
        private const string kMenuMoveToTop = "CONTEXT/Component/Move To Top";
        private const string kMenuMoveToBottom = "CONTEXT/Component/Move To Bottom";

        [MenuItem(kMenuMoveToTop, priority = 501)]
        public static void MoveComponentToTopMenuItem(MenuCommand command)
        {
            while (UnityEditorInternal.ComponentUtility.MoveComponentUp((Component)command.context)) ;
        }

        [MenuItem(kMenuMoveToTop, validate = true)]
        public static bool MoveComponentToTopMenuItemValidate(MenuCommand command)
        {
            Component[] components = ((Component)command.context).gameObject.GetComponents<Component>();

            return !components.Where((t, i) => t == ((Component)command.context) && i == 1).Any();
        }

        [MenuItem(kMenuMoveToBottom, validate = true)]
        public static bool MoveComponentToBottomMenuItemValidate(MenuCommand command)
        {
            Component[] components = ((Component)command.context).gameObject.GetComponents<Component>();

            return !components.Where((t, i) => t == ((Component)command.context) && i == components.Length - 1).Any();
        }

        [MenuItem(kMenuMoveToBottom, priority = 501)]
        public static void MoveComponentToBottomMenuItem(MenuCommand command)
        {
            while (UnityEditorInternal.ComponentUtility.MoveComponentDown((Component)command.context)) ;
        }
    }
}
#endif