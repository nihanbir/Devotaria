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
        private RoomNodeSO _currentRoomNode = null;
        private RoomNodeTypeListSO _roomNodeTypeList;
        
        // Node layout constants
        private const float NodeWidth = 200f;
        private const float NodeHeight = 75f;
        private const int NodePadding = 25;
        private const int NodeBorder = 12;
        
        // Connecting line layout constants
        private const float ConnectingLineWidth = 3f;
        
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
                DrawDraggedLine();
                
                ProcessEvents(Event.current);
                
                DrawRoomNodes();
            }

            if (GUI.changed)
            {
                Repaint();
            }
        }

        private void DrawDraggedLine()
        {
            if (_currentRoomNodeGraph.linePosition != Vector2.zero)
            {
                // Draw line from node to line position
                Handles.DrawBezier(_currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center,
                    _currentRoomNodeGraph.linePosition,
                    _currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, 
                    _currentRoomNodeGraph.linePosition, 
                    Color.white, 
                    null, 
                    ConnectingLineWidth);
            }
        }


        private void ProcessEvents(Event currentEvent)
        {
            
            
            // If the node is null
            // or if the mouse is not currently dragging the node 
            // then set the current room node
            if (!_currentRoomNode || _currentRoomNode.isLeftClickDragging == false)
            {
                _currentRoomNode = GetHoveredRoomNode(currentEvent);
            }
            
            // If the current room node is null,
            // or we are currently dragging a line from the room node
            // then process the room node graph events
            if (!_currentRoomNode || _currentRoomNodeGraph.roomNodeToDrawLineFrom)
            {
                ProcessRoomNodeGraphEvents(currentEvent);
            }
            // Else process the room node events
            else
            {
                _currentRoomNode.ProcessEvents(currentEvent);
            }
        }

        /// <summary>
        /// Return the room node that is currently being hovered over by the mouse
        /// </summary>
        private RoomNodeSO GetHoveredRoomNode(Event currentEvent)
        {
            for (int i = _currentRoomNodeGraph.roomNodeList.Count - 1; i >= 0; i--)
            {
                if (_currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition))
                {
                    return _currentRoomNodeGraph.roomNodeList[i];
                }
            }
            return null;
        }

        private void ProcessRoomNodeGraphEvents(Event currentEvent)
        {
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    ProcessMouseDownEvent(currentEvent);
                    break;
                
                case EventType.MouseUp:
                    ProcessMouseUpEvent(currentEvent);
                    break;
                
                case EventType.MouseDrag:
                    ProcessMouseDragEvent(currentEvent);
                    break;
                
            }
        }

        private void ProcessMouseUpEvent(Event currentEvent)
        {
            // If releasing the right mouse button
            // and currently dragging a line from a room node
            if (currentEvent.button == 1 && _currentRoomNodeGraph.roomNodeToDrawLineFrom)
            {
                ClearLineDrag();
            } 
        }

        private void ClearLineDrag()
        {
            _currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
            _currentRoomNodeGraph.linePosition = Vector2.zero;
            GUI.changed = true;
        }

        private void ProcessMouseDragEvent(Event currentEvent)
        {
            if (currentEvent.button == 1)
            {
                ProcessRightMouseDragEvent(currentEvent);
            }
        }
        
        private void ProcessRightMouseDragEvent(Event currentEvent)
        {
            if (_currentRoomNodeGraph.roomNodeToDrawLineFrom)
            {
                DragConnectingLine(currentEvent.delta);
                GUI.changed = true;
            }
        }

        private void DragConnectingLine(Vector2 currentEventDelta)
        {
            _currentRoomNodeGraph.linePosition += currentEventDelta;
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
