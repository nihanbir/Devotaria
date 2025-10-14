using System.Collections.Generic;
using System.Linq;
using GameManager;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace NodeGraph.Editor
{
    /// <summary>
    /// Represents a custom editor window for managing Room Node Graphs.
    /// This editor window allows users to create, edit, and manage room node graphs
    /// for dungeon or level design in Unity.
    /// </summary>
    /// <remarks>
    /// The RoomNodeGraphEditor integrates with Unity's Editor framework
    /// and provides functionality for handling RoomNodeGraphSO assets.
    /// It includes features like opening the editor window via the Unity menu
    /// or upon double-clicking a RoomNodeGraph asset in the Unity Inspector.
    /// </remarks>
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
        
        // Connection line selection
        private const float ConnectionLineSelectionThreshold = 10f; // Distance threshold for selecting a line
        
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
            
            //Load the room node type list
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
            //If a SO of type RoomNodeGraphSO has been selected, then process
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
                Handles.DrawLine(new Vector3(gridSize * i,  // For each index in our iteration, we multiply that by the grid size to get our position on the x-axis.
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

        /// <summary>
        /// Handles input events for the room node graph editor window, including key presses, mouse interactions,
        /// and determining the active room node or graph-level actions.
        /// </summary>
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
            // or if the mouse is not currently dragging the node, 
            // then set the current room node
            if (!_currentRoomNode || !_currentRoomNode.isLeftClickDragging)
            {
                _currentRoomNode = GetHoveredRoomNode(currentEvent);
            }
            
            // If the current room node is null,
            // or we are currently dragging a line from the room node,
            // then process the room node graph events
            if (!_currentRoomNode || _currentRoomNodeGraph.roomNodeToDrawLineFrom)
            {
                ProcessRoomNodeGraphMouseEvents(currentEvent);
            }
            // Else process the room node events
            else
            {
                _currentRoomNode.ProcessEvents(currentEvent);
            }
        }

        /// <summary>
        /// Processes mouse events for the room node graph editor, such as mouse down, mouse up, and mouse drag.
        /// This method helps manage interactions performed within the graph editor, like selecting and moving nodes.
        /// </summary>
        private void ProcessRoomNodeGraphMouseEvents(Event currentEvent)
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

        /// <summary>
        /// Process mouses down events on the room node graph (not over a node)
        /// </summary>
        private void ProcessMouseDownEvent(Event currentEvent)
        {
            switch (currentEvent.button)
            {
                case 0:
                    ClearLineDrag();
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
                    // Check if we clicked on a connection line
                    if (!TrySelectConnectionLine(currentEvent.mousePosition))
                    {
                        // If we didn't click on a line, clear selections
                        ClearAllSelectedRoomNodes();
                        ClearSelectedConnections();
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

            foreach (var roomNode in _currentRoomNodeGraph.roomNodeList)
            {
                roomNode.DragNode(dragDelta);
            }
            
            GUI.changed = true;
        }
        
        private void ProcessKeyDownEvent(Event currentEvent)
        {
            if (currentEvent.keyCode == KeyCode.Delete)
            {
                // Delete selected connections first, then selected nodes
                if (_currentRoomNodeGraph.selectedConnections.Count > 0)
                {
                    DeleteSelectedConnections();
                }
                else
                {
                    DeleteSelectedRoomNodes();
                }
                // currentEvent.Use();
            }
        }

        /// <summary>
        /// Displays a context menu at the specified mouse position.
        /// The context menu provides options for actions such as adding room nodes,
        /// selecting all room nodes, and deleting selected room node links.
        /// </summary>
        private void ShowContextMenu(Vector2 mousePosition)
        {
            GenericMenu contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Add Room Node"), false, () => AddRoomNode(mousePosition));
            contextMenu.AddSeparator("");
            contextMenu.AddItem(new GUIContent("Select All Room Nodes"), false, SelectAllRoomNodes);
            
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

        /// <summary>
        /// Deletes the links between the selected room nodes and their child nodes.
        /// If a connection to a boss room node is removed, the graph's boss room connection flag is updated accordingly.
        /// </summary>
        private void DeleteSelectedRoomNodeLinks()
        {
            // Iterate through all selected room nodes
            foreach (var roomNode in _currentRoomNodeGraph.roomNodeList.Where(roomNode => roomNode.IsSelected && roomNode.childRoomNodeIDList.Count > 0))
            {
                for (int i = roomNode.childRoomNodeIDList.Count - 1; i >= 0 ; i--)
                {
                    RoomNodeSO childNode = _currentRoomNodeGraph.GetRoomNode(roomNode.childRoomNodeIDList[i]);

                    if (!childNode && !childNode.IsSelected) continue;
                        
                    // If the child node is a boss room, remove the connection to the boss room
                    if (childNode.roomNodeType.isBossRoom || roomNode.roomNodeType.isBossRoom)
                    {
                        _currentRoomNodeGraph.hasConnectedBossRoom = false;
                    }
                        
                    // Remove the connection between the room nodes
                    roomNode.RemoveChildRoomNodeConnection(childNode.id);
                }
            }

            ClearAllSelectedRoomNodes();
        }

        /// <summary>
        /// Deletes the currently selected room nodes from the room node graph, excluding entrance nodes.
        /// Ensures that references to boss room nodes are properly updated during deletion.
        /// This method modifies the state of the room node graph and refreshes the editor.
        /// </summary>
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
    
            // Process-all deletions
            foreach (RoomNodeSO nodeToDelete in nodesToDelete)
            {
                DeleteSelectedRoomNode(nodeToDelete);
            }
            
            AssetDatabase.SaveAssets();
            GUI.changed = true;
        }

        /// <summary>
        /// Deletes the specified room node from the Room Node Graph.
        /// </summary>
        private void DeleteSelectedRoomNode(RoomNodeSO roomNode)
        {
            // Set isSelected to false (this automatically removes from selectedRoomNodes)
            roomNode.IsSelected = false;
            
            _currentRoomNodeGraph.RoomNodeDictionary.Remove(roomNode.id);
            _currentRoomNodeGraph.roomNodeList.Remove(roomNode);
            roomNode.RemoveAllNodeConnections();
            
            DestroyImmediate(roomNode, true);
        }

        /// <summary>
        /// Renders all room nodes in the current Room Node Graph using the assigned node styles.
        /// </summary>
        /// <remarks>
        /// Iterates through the list of room nodes in the selected RoomNodeGraphSO and
        /// draws each one with a unique style depending on its selection state.
        /// </remarks>
        private void DrawRoomNodes()
        {
            foreach (var roomNode in _currentRoomNodeGraph.roomNodeList)
            {
                roomNode.Draw(roomNode.IsSelected ? _roomNodeSelectedStyle : _roomNodeStyle);
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

        /// <summary>
        /// Clears the selection state of all currently selected room nodes in the room node graph.
        /// </summary>
        /// <remarks>
        /// This method iterates through the list of currently selected room nodes, resets their selected state,
        /// and ensures the graphical user interface is updated to reflect these changes. If no nodes are selected,
        /// the method returns immediately without making any modifications.
        /// </remarks>
        private void ClearAllSelectedRoomNodes()
        {
            if (_currentRoomNodeGraph.selectedRoomNodes.Count == 0) return;
            
            // Create a copy to avoid modification during iteration
            var nodesToClear = _currentRoomNodeGraph.selectedRoomNodes.ToList();
            
            foreach (var node in nodesToClear)
            {
                node.IsSelected = false; // This automatically removes from a list
            }
            
            GUI.changed = true;
        }

        /// <summary>
        /// Selects all room nodes in the current Room Node Graph and marks them as selected.
        /// </summary>
        /// <remarks>
        /// This method clears any previously selected room nodes before selecting all nodes in the graph.
        /// It iterates through the list of room nodes available in the current Room Node Graph and sets
        /// each node's 'isSelected' property to true. This ensures that all nodes are added to the selection.
        /// Additionally, it triggers a GUI update to reflect the selection changes within the editor.
        /// </remarks>
        private void SelectAllRoomNodes()
        {
            if (_currentRoomNodeGraph.roomNodeList.Count == 0) return;
            
            ClearAllSelectedRoomNodes();
            
            foreach (var node in _currentRoomNodeGraph.roomNodeList)
            {
                node.IsSelected = true; // This automatically adds to the list
            }
    
            GUI.changed = true;
        }
        #endregion Room Node
        
        #region Room Connection Line

        /// <summary>
        /// Draws visual connection lines between parent room nodes and their child room nodes
        /// in the currently selected RoomNodeGraph. Each connection is represented as a line
        /// to visually demonstrate the hierarchy and relationships between nodes.
        /// </summary>
        /// <remarks>
        /// This method iterates through the list of room nodes in the RoomNodeGraphSO and
        /// checks for relationships between parent and child nodes. If valid relationships
        /// are found, it draws lines connecting the nodes. Changes occur only when connections
        /// exist and are successfully rendered.
        /// </remarks>
        private void DrawRoomConnections()
        {
            // loop through all room nodes
            foreach (var roomNode in _currentRoomNodeGraph.roomNodeList)
            {
                if (roomNode.childRoomNodeIDList.Count <= 0) continue;
                // loop through all child room nodes
                foreach (var childID in roomNode.childRoomNodeIDList.Where(childID => _currentRoomNodeGraph.RoomNodeDictionary.ContainsKey(childID)))
                {
                    DrawConnectionLine(roomNode, _currentRoomNodeGraph.RoomNodeDictionary[childID]);
                    GUI.changed = true;
                }

            }
        }

        /// <summary>
        /// Draw a connection line between the parent node and child node
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
            
            // Check if this connection is selected
            bool isSelected = _currentRoomNodeGraph.selectedConnections.Contains((parentRoomNode.id, childRoomNode.id));
            Color lineColor = isSelected ? Color.yellow : Color.white;
            float lineWidth = isSelected ? ConnectingLineWidth * 1.5f : ConnectingLineWidth;
            
            // Draw the lines from the arrow head to the arrow tail
            Handles.DrawBezier(arrowHeadPoint, arrowTailPoint1, arrowHeadPoint, arrowTailPoint1, lineColor, null, lineWidth);
            Handles.DrawBezier(arrowHeadPoint, arrowTailPoint2, arrowHeadPoint, arrowTailPoint2, lineColor, null, lineWidth);
            
            // Draw a line between the two nodes
            Handles.DrawBezier(start, end, start, end, lineColor, null, lineWidth);

            GUI.changed = true;
        }

        /// <summary>
        /// Draws a bezier line representing a connection being dragged from a room node to the current mouse position, if applicable.
        /// </summary>
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
        
        // <summary>
        /// Tries to select a connection line at the given mouse position.
        /// Returns true if a line was selected, false otherwise.
        /// </summary>
        private bool TrySelectConnectionLine(Vector2 mousePosition)
        {
            foreach (var roomNode in _currentRoomNodeGraph.roomNodeList)
            {
                if (roomNode.childRoomNodeIDList.Count <= 0) continue;
                
                foreach (var childID in roomNode.childRoomNodeIDList)
                {
                    if (!_currentRoomNodeGraph.RoomNodeDictionary.ContainsKey(childID)) continue;
                    
                    RoomNodeSO childNode = _currentRoomNodeGraph.RoomNodeDictionary[childID];
                    Vector2 startPos = roomNode.rect.center;
                    Vector2 endPos = childNode.rect.center;
                    
                    float distance = GetDistanceFromPointToLineSegment(mousePosition, startPos, endPos);
                    
                    if (distance <= ConnectionLineSelectionThreshold)
                    {
                        // Clear previous selections
                        ClearAllSelectedRoomNodes();
                        
                        // Clear previous connection selections
                        ClearSelectedConnections();
                        
                        // Select this connection
                        _currentRoomNodeGraph.selectedConnections.Add((roomNode.id, childID));
                        GUI.changed = true;
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Calculates the distance from a point to a line segment
        /// </summary>
        private float GetDistanceFromPointToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 lineDir = lineEnd - lineStart;
            float lineLength = lineDir.magnitude;
            
            if (lineLength < 0.001f) // Line is essentially a point
                return Vector2.Distance(point, lineStart);
            
            lineDir.Normalize();
            
            Vector2 pointToStart = point - lineStart;
            float projection = Vector2.Dot(pointToStart, lineDir);
            
            // Clamp projection to line segment bounds
            projection = Mathf.Clamp(projection, 0, lineLength);
            Vector2 closestPoint = lineStart + lineDir * projection;
            
            return Vector2.Distance(point, closestPoint);
        }
        
        /// <summary>
        /// Clears all selected connection lines
        /// </summary>
        private void ClearSelectedConnections()
        {
            if (_currentRoomNodeGraph.selectedConnections.Count == 0) return;
            
            _currentRoomNodeGraph.selectedConnections.Clear();
            GUI.changed = true;
        }
        
        /// <summary>
        /// Deletes all currently selected connection lines
        /// </summary>
        private void DeleteSelectedConnections()
        {
            if (_currentRoomNodeGraph.selectedConnections.Count == 0) return;
            
            foreach (var (parentId, childId) in _currentRoomNodeGraph.selectedConnections)
            {
                RoomNodeSO parentNode = _currentRoomNodeGraph.GetRoomNode(parentId);
                RoomNodeSO childNode = _currentRoomNodeGraph.GetRoomNode(childId);
                
                if (parentNode && childNode)
                {
                    // Check if we're removing a boss room connection
                    if (childNode.roomNodeType.isBossRoom || parentNode.roomNodeType.isBossRoom)
                    {
                        _currentRoomNodeGraph.hasConnectedBossRoom = false;
                    }
                    
                    parentNode.RemoveChildRoomNodeConnection(childId);
                }
            }
            
            ClearSelectedConnections();
            AssetDatabase.SaveAssets();
        }
        
        
        #endregion Room Connection Line
        
    }
}
