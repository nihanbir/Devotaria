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
            }

            else
            {
                // Draw popup to select room node type (default to the currently set room node type)
                int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);
                int selection = EditorGUILayout.Popup(selected, GetRoomNodeTypesToDisplay());
                
                roomNodeType = roomNodeTypeList.list[selection];
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
                case 0:
                    ProcessLeftClickDownEvent();
                    break;
                case 1:
                    ProcessRightClickDownEvent(currentEvent);
                    break;
            }
        }
        
        private void ProcessMouseUpEvent(Event currentEvent)
        {
            if (currentEvent.button == 0)
            {
                ProcessLeftClickUpEvent();
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
            
            //.delta captures the mouse movement since the last frame
            DragMode(currentEvent.delta);
            GUI.changed = true;
        }

        private void DragMode(Vector2 currentEventDelta)
        {
            rect.position += currentEventDelta;
            EditorUtility.SetDirty(this);
        }
        
        /// <summary>
        /// Expand this function for left click up events
        /// </summary>
        private void ProcessLeftClickUpEvent()
        {
            if (isLeftClickDragging)
            {
                isLeftClickDragging = false;
            }
        }
        
        /// <summary>
        /// Expand this function for right click down events
        /// </summary>
        private void ProcessRightClickDownEvent(Event currentEvent)
        {
            roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
        }

        /// <summary>
        /// Expand this function for left click down events
        /// </summary>
        private void ProcessLeftClickDownEvent()
        {
            Selection.activeObject = this;
            
            //Toggle node selection
            isSelected = !isSelected;
        }
        #endregion
        
        #region Node Relationships
        /// <summary>
        /// Add childID to the node (returns true if successful)
        /// </summary>
        public bool AddChildRoomNodeIDToRoomNode(string childID)
        {
            // Check child node can be added validly to parent
            if (!isChildRoomValid(childID)) return false;
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
        
        //TODO: Change this to a more sustainable method
        private bool isChildRoomValid(string childID)
        {
            bool isConnectedBossNodeAlready = false;
            
            // Check if there is already a connected boss room in this graph
            foreach (RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
            {
                if (roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count > 0)
                {
                    isConnectedBossNodeAlready = true;
                }
            }

            // If the child node has a type of boss room and there is already a connected boss room in the graph, return false
            if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isBossRoom && isConnectedBossNodeAlready)
                return false;
            
            // If the child node has a type of none then return false
            if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isNone)
                return false;
            
            // If the node already has a child with this child ID, return false
            if (childRoomNodeIDList.Contains(childID))
                return false;
            
            // If this node ID and the child ID are the same, return false
            if (id == childID)
                return false;
            
            // If this childID is already in the parentID list, return false
            if (parentRoomNodeIDList.Contains(childID))
                return false;
            
            // If the child node already has a parent, return false
            if (roomNodeGraph.GetRoomNode(childID).parentRoomNodeIDList.Count > 0)
                return false;
            
            // If the child is a corridor and this node is a corridor, return false
            if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && roomNodeType.isCorridor)
                return false;
            
            // If the child is not a corridor and this node is not a corridor, return false
            if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && !roomNodeType.isCorridor)
                return false;
            
            // If adding a corridor check that this node has less than the maximum permitted child corridors
            if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count >= Settings.maxChildCorridors)
                return false;
            
            // If the child room is an entrance return false - the entrance must always be the top level parent node
            if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isEntrance)
                return false;
            
            // If adding a room to a corridor, check that this corridor node doesn't already have a room added
            if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count > 0)
                return false;
            
            return true;
        }
        
        #endregion Node Relationships
#endif
        
    }
}
