using GameManager;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace NodeGraph.Editor
{
    public class RoomNodeGraphEditor : EditorWindow
    {
        private GUIStyle _roomNodeStyle;
        private GUIStyle _roomNodeSelectedStyle;
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
        private const float ConnectingLineArrowSize = 6f;
        
        //Define where the editor window will be created in the Unity menu
        [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]
        
        public static void OpenWindow()
        {
            GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
        }
        
        private void OnEnable()
        {
            //Subscribe to selection changed event
            Selection.selectionChanged += InspectorSelectionChanged;
            
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
            
            //Define selected node style
            _roomNodeSelectedStyle = new GUIStyle
            {
                normal =
                {
                    background = EditorGUIUtility.Load("node1 on") as Texture2D,
                    textColor = Color.white
                },
                padding = new RectOffset(NodePadding, NodePadding, NodePadding, NodePadding),
                border = new RectOffset(NodeBorder, NodeBorder, NodeBorder, NodeBorder)
            };
            
            //Load room node type list
            _roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
        }

        private void OnDisable()
        {
            //Unsubscribe from selection changed event
            Selection.selectionChanged -= InspectorSelectionChanged;
        }
        
        private void InspectorSelectionChanged()
        {
            RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;
            if (!roomNodeGraph) return;
            _currentRoomNodeGraph = roomNodeGraph;
            GUI.changed = true;
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

                DrawRoomConnections();
                
                DrawRoomNodes();
            }

            if (GUI.changed)
            {
                Repaint();
            }
        }

        private void DrawRoomConnections()
        {
            // loop through all room nodes
            foreach (var roomNode in _currentRoomNodeGraph.roomNodeList)
            {
                if (roomNode.childRoomNodeIDList.Count > 0)
                {
                    // loop through all child room nodes
                    foreach (var childID in roomNode.childRoomNodeIDList)
                    {
                        // get child room node from dictionary
                        if (_currentRoomNodeGraph.roomNodeDictionary.ContainsKey(childID))
                        {
                            DrawConnectionLine(roomNode, _currentRoomNodeGraph.roomNodeDictionary[childID]);
                            GUI.changed = true;
                        }
                    }
                }
               
            }
        }

        /// <summary>
        /// Draw connection line between the parent node and child node
        /// </summary>
        private void DrawConnectionLine(RoomNodeSO parentRoomNode, RoomNodeSO childRoomNode)
        {
            // get the center of the parent and child nodes
            Vector2 start = parentRoomNode.rect.center;
            Vector2 end = childRoomNode.rect.center;
            
            // calculate the midpoint of the line
            Vector2 mid = (start + end) / 2;
            
            // Calculate the direction vector
            Vector2 direction = (end - start).normalized;
            
            // Calculate the perpendicular vector to have the arrow point in the correct direction
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            
            // Calculate the arrow tail points
            Vector2 arrowTailPoint1 = mid + perpendicular * ConnectingLineArrowSize;
            Vector2 arrowTailPoint2 = mid - perpendicular * ConnectingLineArrowSize;
            
            // Calculate the arrow head point
            Vector2 arrowHeadPoint = mid + direction * ConnectingLineArrowSize;
            
            // Draw the lines from the arrow head to the arrow tail
            Handles.DrawBezier(arrowHeadPoint, arrowTailPoint1, arrowHeadPoint, arrowTailPoint1, Color.white, null, ConnectingLineWidth);
            Handles.DrawBezier(arrowHeadPoint, arrowTailPoint2, arrowHeadPoint, arrowTailPoint2, Color.white, null, ConnectingLineWidth);
            
            // Draw line between the two nodes
            Handles.DrawBezier(start, end, start, end, Color.white, null, ConnectingLineWidth);
            
            GUI.changed = true;
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
            if (currentEvent.button != 1 || !_currentRoomNodeGraph.roomNodeToDrawLineFrom) return;
            
            RoomNodeSO roomNode = GetHoveredRoomNode(currentEvent);
                
            if (roomNode)
            {
                // add the child room node to the parent room node
                if (_currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNodeIDToRoomNode(roomNode.id))
                {
                    // if successful, add the parent room node to the child room node
                    roomNode.AddParentRoomNodeIDToRoomNode(_currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
                }
            }
            ClearLineDrag();
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
            switch (currentEvent.button)
            {
                case 0:
                    ClearLineDrag();
                    ClearAllSelectedRoomNodes();
                    break;
                case 1:
                    ShowContextMenu(currentEvent.mousePosition);
                    break;
            }
        }

        private void ClearAllSelectedRoomNodes()
        {
            foreach (RoomNodeSO roomNode in _currentRoomNodeGraph.roomNodeList)
            {
                if (roomNode.isSelected)
                {
                    roomNode.isSelected = false;
                    GUI.changed = true;
                }
            }
        }

        private void ShowContextMenu(Vector2 mousePosition)
        {
            GenericMenu contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Add Room Node"), false, () => AddRoomNode(mousePosition));
            contextMenu.ShowAsContext();
        }

        /// <summary>
        /// Add a new room node at the mouse position with the default room node type
        /// </summary>
        private void AddRoomNode(object mousePositionObject)
        {
            // If there are no room nodes, add an entrance node first
            if (_currentRoomNodeGraph.roomNodeList.Count == 0)
            {
                AddRoomNode(new Vector2(200f, 200f),_roomNodeTypeList.list.Find(x => x.isEntrance));
            }
            
            AddRoomNode(mousePositionObject,_roomNodeTypeList.list.Find(x => x.isNone));
        }
        
        /// <summary>
        /// Add a new room node at the mouse position and with the specified room node type
        /// </summary>
        private void AddRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
        {
            Vector2 mousePosition = (Vector2) mousePositionObject;
            
            RoomNodeSO roomNode = CreateInstance<RoomNodeSO>();
            
            _currentRoomNodeGraph.roomNodeList.Add(roomNode);
            
            roomNode.Initialize(new Rect(mousePosition, new Vector2(NodeWidth, NodeHeight)), _currentRoomNodeGraph, roomNodeType);
            
            AssetDatabase.AddObjectToAsset(roomNode, _currentRoomNodeGraph);
            AssetDatabase.SaveAssets();
            
            // Repopulate the dictionary
            _currentRoomNodeGraph.OnValidate();
        }
        
        private void DrawRoomNodes()
        {
            foreach (var roomNode in _currentRoomNodeGraph.roomNodeList)
            {
                roomNode.Draw(roomNode.isSelected ? _roomNodeSelectedStyle : _roomNodeStyle);
            }

            GUI.changed = true;
        }
    }
}
