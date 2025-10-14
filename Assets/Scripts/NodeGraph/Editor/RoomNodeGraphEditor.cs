using System.Collections.Generic;
using System.Linq;
using GameManager;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace NodeGraph.Editor
{
    public class RoomNodeGraphEditor : EditorWindow
    {
        #region Variables
        
        private GUIStyle _roomNodeStyle;
        private GUIStyle _roomNodeSelectedStyle;
        private static RoomNodeGraphSO _currentRoomNodeGraph;

        private Vector2 _graphOffset;
        private Vector2 _graphDrag;
        
        private RoomNodeSO _currentRoomNode;
        private RoomNodeSO _selectedRoomNode;
        private RoomNodeTypeListSO _roomNodeTypeList;
        
        // Node layout constants
        private const float NodeWidth = 200f;
        private const float NodeHeight = 75f;
        private const int NodePadding = 25;
        private const int NodeBorder = 12;
        
        // Connecting line layout constants
        private const float ConnectingLineWidth = 3f;
        private const float ConnectingLineArrowSize = 6f;
        
        //Grid Spacing
        private const float GridLarge = 100f;
        private const float GridSmall = 25f;
        
        #endregion Variables
        
        #region Graph Editor SO Asset
        //Define where the editor window will be created in the Unity menu
        [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]
        public static void OpenWindow()
        {
            GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
        }
        
        /// <summary>
        /// Open the room node graph editor window if a room node graph SO asset is double-clicked in the inspector
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
        /// Draw editor GUI
        /// </summary>
        private void OnGUI()
        {
            //If a SO of type RoomNodeGraphSO has been selected then process
            if (_currentRoomNodeGraph)
            {
                DrawBackgroundGrid(GridSmall, 0.2f, Color.gray);
                DrawBackgroundGrid(GridLarge, 0.3f, Color.gray);
                
                
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

        /// <summary>
        /// Draw a background grid for the room node graph editor
        /// </summary>
        private void DrawBackgroundGrid(float gridSize, float gridOpacity, Color gridColor)
        {
            int verticalLineCount = Mathf.CeilToInt(position.width + gridSize / gridSize);
            int horizontalLineCount = Mathf.CeilToInt(position.height + gridSize / gridSize);
            
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
            
            // Graph offset based on the graph drag
            // We only need half the graph drag because the grid is symmetrical
            _graphOffset += _graphDrag * 0.5f;
            
            // How much we need to offset each line that we are going to draw
            Vector3 gridOffset = new Vector3(_graphOffset.x % gridSize, _graphOffset.y % gridSize, 0);

           // I need to learn this better, so I tried to take notes on it.
            for (int i = 0; i < verticalLineCount; i++)
            {
                Handles.DrawLine(new Vector3(gridSize * i,  // For each index in our iteration, we multiply that by the grid size to get our position on the x axis.
                                             -gridSize, // And then on the y-axis, we start drawing from minus grid size. This is where we've got our overlap, so we want to start drawing outside the screen slightly.
                                             0) 
                                             + gridOffset, // And then we add in the grid offsets. So this is how much we're offset in the drawing of the line.
                                new Vector3(gridSize * i, 
                                            position.height + gridSize, // Instead of starting in the Y position minus grid size, we end at position height plus grid size. So again, this just creates a bit of overlap in the vertical direction.
                                            0) 
                                            + gridOffset);
               // So this will draw all of our vertical lines, and then we need to do the same for the horizontal lines.
            }
            
            for (int j = 0; j < horizontalLineCount; j++)
            {
                Handles.DrawLine(new Vector3(-gridSize, gridSize * j, 0) + gridOffset, new Vector3(position.width + gridSize, gridSize * j, 0) + gridOffset);
            }
            
            Handles.color = Color.white;
        }

        #endregion Graph Editor SO Asset
        
        #region Event Processors
        private void ProcessEvents(Event currentEvent)
        {
            
            // Reset graph drag
            _graphDrag = Vector2.zero;
            
            // Handle keyboard events at the graph level FIRST (before mouse events)
            if (currentEvent.type == EventType.KeyDown)
            {
                ProcessKeyDownEvent(currentEvent);
                return;
            }
            
            // If the node is null
            // or if the mouse is not currently dragging the node 
            // then set the current room node
            if (!_currentRoomNode || !_currentRoomNode.isLeftClickDragging)
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
                
                // case EventType.KeyDown:
                //     ProcessKeyDownEvent(currentEvent);
                //     break;
                
            }
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
        private void ProcessMouseUpEvent(Event currentEvent)
        {
            RoomNodeSO hoveredRoomNode = GetHoveredRoomNode(currentEvent);
            switch (currentEvent.button)
            {
                case 0:
                    if (hoveredRoomNode)
                    {
                        _selectedRoomNode = hoveredRoomNode;
                        if (!_currentRoomNodeGraph.selectedRoomNodes.Contains(_selectedRoomNode))
                        {
                            _currentRoomNodeGraph.selectedRoomNodes.Add(_selectedRoomNode);
                        }
                        hoveredRoomNode.isSelected = true;
                    }
                    else
                    {
                        ClearAllSelectedRoomNodes();
                    }
                    break;
                case 1:
                    if (_currentRoomNodeGraph.roomNodeToDrawLineFrom)
                    {
                        if (hoveredRoomNode)
                        { 
                            _currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNodeConnection(hoveredRoomNode.id);
                        }
                        ClearLineDrag();
                    }
                    break;
            }
        }
        
        private void ProcessMouseDragEvent(Event currentEvent)
        {
            switch (currentEvent.button)
            {
                case 0:
                    ProcessLeftMouseDragEvent(currentEvent.delta);
                    break;
                case 1:
                    ProcessRightMouseDragEvent(currentEvent);
                    break;
                
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
        
        private void ProcessLeftMouseDragEvent(Vector2 dragDelta)
        {
            _graphDrag = dragDelta;

            for (int i = 0; i < _currentRoomNodeGraph.roomNodeList.Count; i++)
            {
                _currentRoomNodeGraph.roomNodeList[i].DragNode(dragDelta);
            }
            
            GUI.changed = true;
        }
        
        private void ProcessKeyDownEvent(Event currentEvent)
        {
            if (currentEvent.keyCode == KeyCode.Delete)
            {
                DeleteSelectedRoomNodes();
                // currentEvent.Use();
            }
        }

        private void ShowContextMenu(Vector2 mousePosition)
        {
            GenericMenu contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Add Room Node"), false, () => AddRoomNode(mousePosition));
            contextMenu.AddSeparator("");
            contextMenu.AddItem(new GUIContent("Select All Room Nodes"), false, SelectAllRoomNodes);
            
            //TODO: Change this to buttons instead of menu items
            contextMenu.AddSeparator("");
            contextMenu.AddItem(new GUIContent("Delete Selected Room Node Links"), false, DeleteSelectedRoomNodeLinks);
            
            contextMenu.ShowAsContext();
        }
        #endregion Event Processors
        
        #region Room Node
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

        private void DeleteSelectedRoomNodeLinks()
        {
            // Itarate through all selected room nodes
            foreach (RoomNodeSO roomNode in _currentRoomNodeGraph.roomNodeList)
            {
                // If the room node is selected
                if (roomNode.isSelected && roomNode.childRoomNodeIDList.Count > 0)
                {
                    for (int i = roomNode.childRoomNodeIDList.Count - 1; i >= 0 ; i--)
                    {
                        RoomNodeSO childNode = _currentRoomNodeGraph.GetRoomNode(roomNode.childRoomNodeIDList[i]);

                        if (!childNode && !childNode.isSelected) continue;
                        
                        // If the child node is a boss room, remove the connection to the boss room
                        if (childNode.roomNodeType.isBossRoom || roomNode.roomNodeType.isBossRoom)
                        {
                            _currentRoomNodeGraph.hasConnectedBossRoom = false;
                        }
                        
                        // Remove the connection between the room nodes
                        roomNode.RemoveChildRoomNodeConnection(childNode.id);
                    }
                }
            }
            ClearAllSelectedRoomNodes();
        }
        
        private void DeleteSelectedRoomNodes()
        {
            // Use the graph's selected nodes list
            if (_currentRoomNodeGraph.selectedRoomNodes.Count == 0) return;
            
            // Filter out entrance nodes upfront
            List<RoomNodeSO> nodesToDelete = _currentRoomNodeGraph.selectedRoomNodes
                .Where(roomNode => !roomNode.roomNodeType.isEntrance)
                .ToList();
    
            // Early return if nothing to delete
            if (nodesToDelete.Count == 0) return;
    
            // Check for boss rooms
            bool hasBossRoom = nodesToDelete.Any(node => node.roomNodeType.isBossRoom);
            if (hasBossRoom)
            {
                _currentRoomNodeGraph.hasConnectedBossRoom = false;
            }
    
            // Process all deletions
            foreach (RoomNodeSO nodeToDelete in nodesToDelete)
            {
                DeleteSelectedRoomNode(nodeToDelete);
            }
            
            AssetDatabase.SaveAssets();
            GUI.changed = true;
        }

        private void DeleteSelectedRoomNode(RoomNodeSO roomNode)
        {
            // Set isSelected to false (this automatically removes from selectedRoomNodes)
            roomNode.isSelected = false;
            
            _currentRoomNodeGraph.RoomNodeDictionary.Remove(roomNode.id);
            _currentRoomNodeGraph.roomNodeList.Remove(roomNode);
            roomNode.RemoveAllNodeConnections();
            
            DestroyImmediate(roomNode, true);
        }
        private void DrawRoomNodes()
        {
            foreach (var roomNode in _currentRoomNodeGraph.roomNodeList)
            {
                roomNode.Draw(roomNode.isSelected ? _roomNodeSelectedStyle : _roomNodeStyle);
            }

            GUI.changed = true;
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

        private void ClearAllSelectedRoomNodes()
        {
            if (_currentRoomNodeGraph.selectedRoomNodes.Count == 0) return;
            
            // Create a copy to avoid modification during iteration
            var nodesToClear = _currentRoomNodeGraph.selectedRoomNodes.ToList();
            
            foreach (var node in nodesToClear)
            {
                node.isSelected = false; // This automatically removes from list
            }
            
            GUI.changed = true;
        }
        
        private void SelectAllRoomNodes()
        {
            if (_currentRoomNodeGraph.roomNodeList.Count == 0) return;
            
            ClearAllSelectedRoomNodes();
            
            foreach (var node in _currentRoomNodeGraph.roomNodeList)
            {
                node.isSelected = true; // This automatically adds to list
            }
    
            GUI.changed = true;
        }
        #endregion Room Node
        
        #region Room Connection Line
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
                        if (_currentRoomNodeGraph.RoomNodeDictionary.ContainsKey(childID))
                        {
                            DrawConnectionLine(roomNode, _currentRoomNodeGraph.RoomNodeDictionary[childID]);
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

        /// <summary>
        /// Clear connection line
        /// </summary>
        private void ClearLineDrag()
        {
            _currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
            _currentRoomNodeGraph.linePosition = Vector2.zero;
            GUI.changed = true;
        }
        
        private void DragConnectingLine(Vector2 currentEventDelta)
        {
            _currentRoomNodeGraph.linePosition += currentEventDelta;
        }
        #endregion Room Connection Line

        
        
        
    }
}
