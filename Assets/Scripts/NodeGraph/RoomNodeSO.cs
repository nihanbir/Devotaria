using System;
using System.Collections.Generic;
using GameManager;
using UnityEditor;
using UnityEngine;

namespace NodeGraph
{
    public class RoomNodeSO : ScriptableObject
    {
        [HideInInspector] public string id;
        [HideInInspector] public List<string> parentRoomNodeIDList = new List<string>();
        [HideInInspector] public List<string> childRoomNodeIDList = new List<string>();
        [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
        public RoomNodeTypeSO roomNodeType;
        [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;
        
        #region Editor Code
#if UNITY_EDITOR

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
            
            // Draw popup to select room node type (default to the currently set room node type)
            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);
            int selection = EditorGUILayout.Popup(selected, GetRoomNodeTypesToDisplay());
            
            roomNodeType = roomNodeTypeList.list[selection];
            
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
            }
        }

        private void ProcessMouseDownEvent(Event currentEvent)
        {
            if (currentEvent.button == 0)
            {
                ProcessLeftClickDownEvent();
            }
            else if (currentEvent.button == 1)
            {
                ProcessRightClickDownEvent(currentEvent);
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

        /// <summary>
        /// Add childID to the node (returns true if successful)
        /// </summary>
        
        public bool AddChildRoomNodeIDToRoomNode(string childID)
        {
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
#endif
        #endregion
    }
}
