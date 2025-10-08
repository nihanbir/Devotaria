using System;
using UnityEditor;
using UnityEngine;

namespace NodeGraph.Editor
{
    public class RoomNodeGraphEditor : EditorWindow
    {
        private GUIStyle _roomNodeStyle;
        
        private const float NodeWidth = 160f;
        private const float NodeHeight = 75f;
        private const int NodePadding = 25;
        private const int NodeBorder = 12;
        
        [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]
        
        public static void ShowWindow()
        {
            GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
        }

        private void OnEnable()
        {
            //Define node layout style
            _roomNodeStyle = new GUIStyle
            {
                normal =
                {
                    background = EditorGUIUtility.Load("node1") as Texture2D,
                    textColor = Color.white
                },
                padding = new RectOffset(NodePadding, NodePadding, NodePadding, NodePadding),
                border = new RectOffset(NodeBorder, NodeBorder, NodeBorder, NodeBorder)
            };
        }

        private void OnGUI()
        {
            //dummy node
            GUILayout.BeginArea(new Rect(new Vector2(100f,100f), new Vector2(NodeWidth, NodeHeight)), _roomNodeStyle);
            
            EditorGUILayout.LabelField("Node 1");
            GUILayout.EndArea();
            
        }
    }
}
