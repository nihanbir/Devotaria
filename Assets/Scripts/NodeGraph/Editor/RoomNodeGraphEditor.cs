using GameManager;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace NodeGraph.Editor
{
    public class RoomNodeGraphEditor : EditorWindow
    {
        private GUIStyle _roomNodeStyle;
        private static RoomNodeGraphSO _currentRoomNodeGraph;
        private RoomNodeTypeListSO _roomNodeTypeList;
        
        private const float NodeWidth = 200f;
        private const float NodeHeight = 75f;
        private const int NodePadding = 25;
        private const int NodeBorder = 12;
        
        //Define where the editor window will be created in the Unity menu
        [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]
        
        public static void OpenWindow()
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
            
            //Load room node type list
            _roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
        }

        /// <summary>
        /// Open the room node graph editor window if a room node graph scriptable object asset is double-clicked in the inspector
        /// </summary>
        [OnOpenAsset(0)] //First callback to be executed when the asset is double-clicked
        public static bool OnDoubleClickAsset(int instanceID, int line)
        {
            RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;
            if (roomNodeGraph == null) return false;
            
            OpenWindow();
            _currentRoomNodeGraph = roomNodeGraph;
            return true;
        }
        
        /// <summary>
        /// Draw editor GUI
        /// </summary>
        private void OnGUI()
        {
            //If a scriptable object of type RoomNodeGraphSO has been selected then process
            if (_currentRoomNodeGraph)
            {
                ProcessEvents(Event.current);
                DrawRoomNodes();
            }

            if (GUI.changed)
            {
                Repaint();
            }
        }

        

        private void ProcessEvents(Event currentEvent)
        {
            ProcessRoomNodeGraphEvents(currentEvent);
        }

        private void ProcessRoomNodeGraphEvents(Event currentEvent)
        {
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    ProcessMouseDownEvent(currentEvent);
                    break;
               
            }
        }
        
        /// <summary>
        /// Process mouse down events on the room node graph (not over a node)
        /// </summary>
        private void ProcessMouseDownEvent(Event currentEvent)
        {
            if (currentEvent.button == 1)
            {
                ShowContextMenu(currentEvent.mousePosition);
            }
        }

        private void ShowContextMenu(Vector2 mousePosition)
        {
            GenericMenu contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Add Room Node"), false, () => AddRoomNode(mousePosition));
            contextMenu.ShowAsContext();
        }

        /// <summary>
        /// Create a new room node at the mouse position with the default room node type
        /// </summary>
        private void AddRoomNode(object mousePositionObject)
        {
            AddRoomNode(mousePositionObject,_roomNodeTypeList.list.Find(x => x.isNone));
        }
        
        /// <summary>
        /// Create a new room node at the mouse position and with the specified room node type
        /// </summary>
        private void AddRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
        {
            Vector2 mousePosition = (Vector2) mousePositionObject;
            
            RoomNodeSO roomNode = CreateInstance<RoomNodeSO>();
            
            _currentRoomNodeGraph.roomNodeList.Add(roomNode);
            
            roomNode.Initialize(new Rect(mousePosition, new Vector2(NodeWidth, NodeHeight)), _currentRoomNodeGraph, roomNodeType);
            
            AssetDatabase.AddObjectToAsset(roomNode, _currentRoomNodeGraph);
            AssetDatabase.SaveAssets();
        }
        
        private void DrawRoomNodes()
        {
            foreach (var roomNode in _currentRoomNodeGraph.roomNodeList)
            {
                roomNode.Draw(_roomNodeStyle);
            }

            GUI.changed = true;
        }
    }
}
