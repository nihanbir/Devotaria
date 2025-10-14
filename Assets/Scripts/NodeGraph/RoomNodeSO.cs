using System;
using System.Collections.Generic;
using System.Linq;
using GameManager;
using Misc;
using UnityEditor;
using UnityEngine;

namespace NodeGraph
{
    public class RoomNodeSO : ScriptableObject
    {
        #region Variables
        [HideInInspector] public string id;
        [HideInInspector] public List<string> parentRoomNodeIDList = new List<string>();
        [HideInInspector] public List<string> childRoomNodeIDList = new List<string>();
        [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
        public RoomNodeTypeSO roomNodeType;
        [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;
        
        #endregion Variables
        
#if UNITY_EDITOR
        #region Editor Code

        [HideInInspector] public Rect rect;
        [HideInInspector] public bool isLeftClickDragging;
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return; // No change, skip
                
                _isSelected = value;
                
                // Update the graph's selected nodes list
                if (roomNodeGraph)
                {
                    if (_isSelected)
                    {
                        if (!roomNodeGraph.selectedRoomNodes.Contains(this))
                        {
                            roomNodeGraph.selectedRoomNodes.Add(this);
                            roomNodeGraph.selectedConnections.Clear();
                        }
                    }
                    else
                    {
                        roomNodeGraph.selectedRoomNodes.Remove(this);
                    }
                }
            }
        }
        
        public void Initialize(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
        {
            this.rect = rect;
            id = Guid.NewGuid().ToString();
            name = "Room Node";
            roomNodeGraph = nodeGraph;
            this.roomNodeType = roomNodeType;
            
            // Set the name based on room node type
            if (roomNodeType)
            {
                name = roomNodeType.roomNodeTypeName;
            }
            
            //Load the room node type list
            roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
        }
        
        /// <summary>
        /// Draw a node with the node style
        /// </summary>
        public void Draw(GUIStyle nodeStyle)
        {
            // Draw the node box
            GUILayout.BeginArea(rect, nodeStyle);
            
            // Start region to detect popup selection changes
            EditorGUI.BeginChangeCheck();
            
            // If the room node has a parent or is of type entrance, then display a label else display a popup
            if (parentRoomNodeIDList.Count > 0 || roomNodeType.isEntrance)
            {
                // Display a label that can't be changed
                EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
                
                if (roomNodeType.isBossRoom)
                {
                    roomNodeGraph.hasConnectedBossRoom = true;
                }
            }

            else
            {
                // Draw popup to select room node type (default to the currently set room node type)
                int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);
                int selection = EditorGUILayout.Popup(selected, GetRoomNodeTypesToDisplay());
                
                roomNodeType = roomNodeTypeList.list[selection];
                
                // Update ScriptableObject name when room node type changes
                if (selected != selection && roomNodeType)
                {
                    name = roomNodeType.roomNodeTypeName;
                    AssetDatabase.SaveAssets();
                }

                // If the room node type has changed, check if the room node can be connected to the current room node
                if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor
                    || !roomNodeTypeList.list[selected].isCorridor && roomNodeTypeList.list[selection].isCorridor
                    || !roomNodeTypeList.list[selected].isBossRoom && roomNodeTypeList.list[selection].isBossRoom)
                {
                    if (childRoomNodeIDList.Count > 0)
                    {
                        for (int i = childRoomNodeIDList.Count - 1; i >= 0 ; i--)
                        {
                            RoomNodeSO childNode = roomNodeGraph.GetRoomNode(childRoomNodeIDList[i]);

                            if (!childNode) continue;
                        
                            // If the child node is a boss room, remove the connection to the boss room
                            if (childNode.roomNodeType.isBossRoom || roomNodeType.isBossRoom)
                            {
                                roomNodeGraph.hasConnectedBossRoom = false;
                            }
                        
                            // Remove childID from the room node
                            RemoveNodeIDFromList(childRoomNodeIDList, childNode.id);
                            
                            // Remove parentID from the child room node
                            childNode.RemoveNodeIDFromList(childNode.parentRoomNodeIDList, id);
                        }
                    }
                }
            }
                
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(this);
            }
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Populate a string array with the room node types to display in the popup
        /// </summary>
        private string[] GetRoomNodeTypesToDisplay()
        {
            string[] roomArray = new string[roomNodeTypeList.list.Count];

            for (int i = 0; i < roomNodeTypeList.list.Count; i++)
            {
                if (roomNodeTypeList.list[i].displayInNodeGraphEditor)
                {
                    roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName;
                }
            }
            return roomArray;
        }

        #endregion
        
        #region Event Processors
        public void ProcessEvents(Event currentEvent)
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
        
        private void ProcessMouseDownEvent(Event currentEvent)
        {
            switch (currentEvent.button)
            {
                case 1:
                    ProcessRightClickDownEvent(currentEvent);
                    break;
            }
        }
        
        /// <summary>
        /// Expand this function for right-click down events
        /// </summary>
        private void ProcessRightClickDownEvent(Event currentEvent)
        {
            roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
        }
        
        private void ProcessMouseUpEvent(Event currentEvent)
        {
            if (currentEvent.button == 0)
            {
                ProcessLeftClickUpEvent(currentEvent);
            }
        }
        
        /// <summary>
        /// Expand this function for left click up events
        /// </summary>
        private void ProcessLeftClickUpEvent(Event currentEvent)
        {
            if (isLeftClickDragging)
            {
                isLeftClickDragging = false;
                return;
            }
            
            // Check if Shift is held down
            if (currentEvent.shift)
            {
                HandleMultiSelect();
            }
            else
            {
                HandleSingleSelect(); 
            }
    
            UpdateUnitySelection();
        }

        /// <summary>
        /// Handles multi-selection mode (Shift + Click) - toggles this node's selection
        /// </summary>
        private void HandleMultiSelect()
        {
            IsSelected = !IsSelected;
        }
        
        /// <summary>
        /// Handles single selection mode (Click) - selects only this node
        /// </summary>
        private void HandleSingleSelect()
        {
            // If no nodes are selected, just select this one
            if (roomNodeGraph.selectedRoomNodes.Count == 0)
            {
                IsSelected = true;
                return;
            }
            // Single select mode: check if this is already the only selected node
            if (IsSelected && roomNodeGraph.selectedRoomNodes.Count == 1)
            {
                // Already the only selected node, do nothing
                return;
            }
                
            // Clear all others
            ClearOtherSelections();
           
            // Select this node (only if not already selected)
            if (!IsSelected)
            {
                IsSelected = true;
            }
        }
        
        /// <summary>
        /// Clears all selected nodes except this one
        /// </summary>
        private void ClearOtherSelections()
        {
            if (!roomNodeGraph) return;
    
            // Clear all other selected nodes except this one
            var nodesToClear = roomNodeGraph.selectedRoomNodes.ToList();
            foreach (var node in nodesToClear.Where(node => node != this))
            {
                node.IsSelected = false;
            }
        }
        
        /// <summary>
        /// Updates Unity's Selection.activeObject to match this node's selection state
        /// </summary>
        private void UpdateUnitySelection()
        {
            if (IsSelected)
            {
                if (!Selection.Contains(this))
                {
                    Selection.activeObject = this;
                }
            }
            else
            {
                Selection.activeObject = null;
            }
        }
        
        private void ProcessMouseDragEvent(Event currentEvent)
        {
            if (currentEvent.button == 0)
            {
                ProcessLeftMouseDragEvent(currentEvent);
            }
        }
        
        /// <summary>
        /// Expand this function for left mouse drag events
        /// </summary>
        private void ProcessLeftMouseDragEvent(Event currentEvent)
        {
            isLeftClickDragging = true;
            
            // If this node is not selected, clear other selections and select only this one
            if (!IsSelected)
            {
                ClearOtherSelections();
                IsSelected = true;
            }
            
            if (!Selection.Contains(this))
            {
                Selection.activeObject = this;
            }
            
            // Drag all selected nodes together
            DragSelectedNodes(currentEvent.delta);
            
            // //.delta have captured the mouse movement since the last frame
            // DragNode(currentEvent.delta);
            GUI.changed = true;
        }

        /// <summary>
        /// Drags all currently selected nodes by the specified delta
        /// </summary>
        private void DragSelectedNodes(Vector2 delta)
        {
            if (!roomNodeGraph) return;
            
            foreach (var node in roomNodeGraph.selectedRoomNodes)
            {
                node.DragNode(delta);
            }
        }

        public void DragNode(Vector2 currentEventDelta)
        {
            rect.position += currentEventDelta;
            EditorUtility.SetDirty(this);
        }
        #endregion
        
        #region Node Connections

        /// <summary>
        /// Add a child node to this node (validates and maintains bidirectional relationship)
        /// </summary>
        public bool AddChildRoomNodeConnection(string childID)
        {
            // // Validate the child can be added (includes null check)
            if (!IsChildRoomValid(childID)) return false;
            
            RoomNodeSO childNode = roomNodeGraph.GetRoomNode(childID);

            // Add to both sides of the relationship
            bool childAdded = AddNodeIDToList(childRoomNodeIDList, childID);
            bool parentAdded = childNode.AddNodeIDToList(childNode.parentRoomNodeIDList, id);

            return childAdded && parentAdded;
        }

        /// <summary>
        /// Remove a child node from this node (maintains bidirectional relationship)
        /// </summary>
        public bool RemoveChildRoomNodeConnection(string childID)
        {
            RoomNodeSO childNode = roomNodeGraph.GetRoomNode(childID);
            if (!childNode) return false;

            bool childRemoved = RemoveNodeIDFromList(childRoomNodeIDList, childID);
            bool parentRemoved = childNode.RemoveNodeIDFromList(childNode.parentRoomNodeIDList, id);

            return childRemoved || parentRemoved;
        }
        
        /// <summary>
        /// Remove all relationships this node has with parent and child nodes
        /// </summary>
        public bool RemoveAllNodeConnections()
        {
            bool allSuccessful = true;
            
            foreach (string childID in childRoomNodeIDList.ToList())
            {
                // RemoveChildRoomNodeConnection validates the childID is valid
                if (!RemoveChildRoomNodeConnection(childID))
                {
                    allSuccessful = false;
                }
            }
                    
            foreach (string parentID in parentRoomNodeIDList.ToList())
            {
                RoomNodeSO parentNode = roomNodeGraph.GetRoomNode(parentID);
                if (!parentNode) continue;
                if (!parentNode.RemoveChildRoomNodeConnection(id))
                {
                    allSuccessful = false;
                }           
            }
            
            return allSuccessful;       
        }
        
        /// <summary>
        /// Helper method to add an ID to a list (returns true if successfully added)
        /// </summary>
        private bool AddNodeIDToList(List<string> list, string idToAdd)
        {
            if (list.Contains(idToAdd)) return false;
            list.Add(idToAdd);
            return true;
        }
        
        /// <summary>
        /// Helper method to remove an ID from a list (returns true if the ID was found and removed)
        /// </summary>
        private bool RemoveNodeIDFromList(List<string> list, string idToRemove)
        {
            if (!list.Contains(idToRemove)) return false;
            list.Remove(idToRemove);
            return true;
        }
        private bool IsChildRoomValid(string childID)
        {
            RoomNodeSO childNode = roomNodeGraph.GetRoomNode(childID);
            if (!childNode)
                return false;

            // Early validation checks
            if (!IsChildNodeStructurallyValid(childID, childNode))
                return false;

            // Room type compatibility checks
            if (!AreRoomTypesCompatible(childNode))
                return false;

            // Boss room validation
            if (childNode.roomNodeType.isBossRoom && roomNodeGraph.hasConnectedBossRoom)
                return false;

            return true;
        }
        private bool IsChildNodeStructurallyValid(string childID, RoomNodeSO childNode)
        {
            // If the child node is not assigned a type, return false
            if (childNode.roomNodeType.isNone)
                return false;
            
            // If the child node is an entrance, return false
            if (childNode.roomNodeType.isEntrance)
                return false;
            
            // If the node and the child node are the same, return false
            if (id == childID)
                return false;
            
            // If the child node already has a parent, return false
            if (childNode.parentRoomNodeIDList.Count > 0)
                return false;
            
            // If the child node is the parent of this node, return false
            if (parentRoomNodeIDList.Contains(childID))
                return false;
            
            //TODO: I want to keep the next check here for when I print the error messages
            
            // If the child node is already a child of this node, return false
            if (childRoomNodeIDList.Contains(childID))
                return false;

            return true;
        }
        
        private bool AreRoomTypesCompatible(RoomNodeSO childNode)
        {
            bool childIsCorridor = childNode.roomNodeType.isCorridor;
            bool thisIsCorridor = roomNodeType.isCorridor;

            // Both nodes cannot be the same type (both corridors or both rooms)
            if (childIsCorridor == thisIsCorridor)
                return false;

            // If this is a corridor, it can only have one child
            if (thisIsCorridor && childRoomNodeIDList.Count > 0)
                return false;

            // If adding a corridor to a room, check the max corridor limit
            if (childIsCorridor && childRoomNodeIDList.Count >= Settings.maxChildCorridors)
                return false;

            return true;
        }
        
        #endregion Node Connections
#endif
        
    }
}
