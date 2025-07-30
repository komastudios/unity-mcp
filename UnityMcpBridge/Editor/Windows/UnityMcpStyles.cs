using UnityEditor;
using UnityMcpBridge.Editor.Models;
using UnityEngine;

namespace UnityMcpBridge.Editor.Windows
{
    public static class UnityMcpStyles
    {

        private static GUIStyle s_TitleLabel;
        public static GUIStyle TitleLabel => s_TitleLabel ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 10, 10)
        };

        private static GUIStyle s_HeaderLabel;
        public static GUIStyle HeaderLabel => s_HeaderLabel ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            padding = new RectOffset(5, 5, 5, 5),
            wordWrap = true
        };

        private static GUIStyle s_BoldLabel;
        public static GUIStyle BoldLabel => s_BoldLabel ??= new GUIStyle(EditorStyles.boldLabel);

        private static GUIStyle s_WrappedLabel;
        public static GUIStyle WrappedLabel => s_WrappedLabel ??= new GUIStyle(EditorStyles.label) { wordWrap = true };

        private static GUIStyle s_Box;
        public static GUIStyle Box => s_Box ??= new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(5, 5, 5, 5)
        };

        private static GUIStyle s_Button;
        public static GUIStyle Button => s_Button ??= new GUIStyle(GUI.skin.button)
        {
            padding = new RectOffset(15, 15, 8, 8),
            margin = new RectOffset(5, 5, 5, 5),
            fontSize = 12
        };

        private static GUIStyle s_MutedButton;
        public static GUIStyle MutedButton => s_MutedButton ??= new GUIStyle(Button)
        {
            normal = { textColor = Color.gray },
            hover = { textColor = Color.white }
        };

        private static GUIStyle s_SmallButton;
        public static GUIStyle SmallButton => s_SmallButton ??= new GUIStyle(GUI.skin.button)
        {
            padding = new RectOffset(10, 10, 5, 5),
            margin = new RectOffset(2, 2, 2, 2),
            fontSize = 10
        };

        public static readonly Color Green = new Color(0.2f, 0.8f, 0.2f);
        public static readonly Color Yellow = new Color(0.8f, 0.8f, 0.2f);
        public static readonly Color Red = new Color(0.8f, 0.2f, 0.2f);
        public static readonly Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.1f);



        public static Color GetStatusColor(McpStatus status)
        {
            return status switch
            {
                McpStatus.Configured => Green,
                McpStatus.Running => Green,
                McpStatus.Connected => Green,
                McpStatus.IncorrectPath => Yellow,
                McpStatus.CommunicationError => Yellow,
                McpStatus.NoResponse => Yellow,
                _ => Red,
            };
        }

        public static void DrawStatusDot(Rect position, Color color)
        {
            Rect dotRect = new Rect(position.x + 6, position.y + 4, 12, 12);
            Vector3 center = new Vector3(dotRect.x + dotRect.width / 2, dotRect.y + dotRect.height / 2, 0);
            float radius = dotRect.width / 2;

            Handles.color = color;
            Handles.DrawSolidDisc(center, Vector3.forward, radius);

            Color borderColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
            Handles.color = borderColor;
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }
    }
}