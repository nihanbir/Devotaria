using System;
using System.Collections.Generic;
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
        [HideInInspector] public bool isSelected;
        
        public void Initialize(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
        {
            this.rect = rect;
            id = Guid.NewGuid().ToString();
            name = "Room Node";
            roomNodeGraph = nodeGraph;
            this.roomNodeType = roomNodeType;
            
            //Load room node type list
            roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
        }
        
        /// <summary>
        /// Draw node with the nodestyle
        /// </summary>
        public void Draw(GUIStyle nodeStyle)
        {
            // Draw node box
            GUILayout.BeginArea(rect, nodeStyle);
            
            // Start region to detect popup selection changes
            EditorGUI.BeginChangeCheck();
            
            // If the room node has a parent or is of type entrance then display a label else display a popup
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
                            RemoveChildRoomNodeIDFromRoomNode(childNode.id);
                            
                            // Remove parentID from the child room node
                            childNode.RemoveParentRoomNodeIDFromRoomNode(id);
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
        public string[] GetRoomNodeTypesToDisplay()
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
        /// Expand this function for right click down events
        /// </summary>
        private void ProcessRightClickDownEvent(Event currentEvent)
        {
            roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
        }
        
        private void ProcessMouseUpEvent(Event currentEvent)
        {
            if (currentEvent.button == 0)
            {
                ProcessLeftClickUpEvent();
            }
        }
        
        /// <summary>
        /// Expand this function for left click up events
        /// </summary>
        private void ProcessLeftClickUpEvent()
        {
            if (isLeftClickDragging)
            {
                isLeftClickDragging = false;
                return;
            }

            if (isSelected)
            {
                isSelected = false;
                Selection.activeObject = null;
            }
            else
            {
                isSelected = true;
                if (!Selection.Contains(this))
                {
                    Selection.activeObject = this;
                }
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
            if (!Selection.Contains(this))
            {
                Selection.activeObject = this;
            }
            isSelected = true;
            
            //.delta captures the mouse movement since the last frame
            DragMode(currentEvent.delta);
            GUI.changed = true;
        }

        private void DragMode(Vector2 currentEventDelta)
        {
            rect.position += currentEventDelta;
            EditorUtility.SetDirty(this);
        }
        #endregion
        
        #region Node Relationships
        
        /// <summary>
        /// Add childID to the node (returns true if successful)
        /// </summary>
        public bool AddChildRoomNodeIDToRoomNode(string childID)
        {
            // Check child node can be added validly to parent
            if (!IsChildRoomValid(childID)) return false;
            childRoomNodeIDList.Add(childID);
            return true;

        }

        /// <summary>
        /// Add parentID to the node (returns true if successful)
        /// </summary>
        public bool AddParentRoomNodeIDToRoomNode(string parentID)
        {
            parentRoomNodeIDList.Add(parentID);
            return true;
        }

        /// <summary>
        /// Remove childID from the node (returns true if successful)
        /// </summary>
        public bool RemoveChildRoomNodeIDFromRoomNode(string childID)
        {
            if (!childRoomNodeIDList.Contains(childID)) return false;
            childRoomNodeIDList.Remove(childID);
            return true;
        }

        /// <summary>
        /// Remove parentID from the node (returns true if successful)
        /// </summary>
        public bool RemoveParentRoomNodeIDFromRoomNode(string parentID)
        {
            if (!parentRoomNodeIDList.Contains(parentID)) return false;
            parentRoomNodeIDList.Remove(parentID);
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

            // If adding a corridor to a room, check max corridor limit
            if (childIsCorridor && childRoomNodeIDList.Count >= Settings.maxChildCorridors)
                return false;

            return true;
        }
        
        #endregion Node Relationships
#endif
        
    }
}
