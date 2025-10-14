using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{
    
    //Define where the SO will be created in the Unity menu
    [CreateAssetMenu(fileName = "RoomNodeGraphSO", menuName = "Scriptable Objects/Dungeon/Room Node Graph")]
    public class RoomNodeGraphSO : ScriptableObject
    {
        [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;
        [HideInInspector] public List<RoomNodeSO> roomNodeList = new List<RoomNodeSO>();
        [HideInInspector] public bool hasConnectedBossRoom;
        
        // Collection for tracking selected room nodes
        [HideInInspector] public List<RoomNodeSO> selectedRoomNodes = new List<RoomNodeSO>();

        //Create a dictionary to store the room nodes by unique GUID
        public readonly Dictionary<string, RoomNodeSO> RoomNodeDictionary = new Dictionary<string, RoomNodeSO>();

        private void Awake()
        {
            LoadRoomNodeDictionary();
        }

        private void LoadRoomNodeDictionary()
        {
            // Populate the dictionary with the room nodes
            foreach (var roomNode in roomNodeList)
            {
                RoomNodeDictionary[roomNode.id] = roomNode;
            }
        }

        /// <summary>
        /// Get room node by room nodeID
        /// </summary>
        public RoomNodeSO GetRoomNode(string roomNodeID)
        {
            return RoomNodeDictionary.GetValueOrDefault(roomNodeID);
        }
        
        #region Editor Code
#if UNITY_EDITOR
        [HideInInspector] public RoomNodeSO roomNodeToDrawLineFrom;
        [HideInInspector] public Vector2 linePosition;

        // Repopulate the dictionary when the SO is edited in the editor
        public void OnValidate()
        {
            LoadRoomNodeDictionary();
        }
        
        public void SetNodeToDrawConnectionLineFrom(RoomNodeSO roomNode, Vector2 position)
        {
            roomNodeToDrawLineFrom = roomNode;
            linePosition = position;
        }
#endif

        #endregion
    }
}
