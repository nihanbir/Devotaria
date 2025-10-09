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
        
#endif
        #endregion
    }
}
