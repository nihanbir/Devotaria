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
        
        //Create a dictionary to store the room nodes by unique GUID
        [HideInInspector] public Dictionary<string, RoomNodeSO> roomNodeDictionary = new Dictionary<string, RoomNodeSO>();
        
        #region Editor Code
#if UNITY_EDITOR
        [HideInInspector] public RoomNodeSO roomNodeToDrawLineFrom = null;
        [HideInInspector] public Vector2 linePosition;
        [HideInInspector] public RoomNodeSO roomNodeToDrawLineTo = null;

        public void SetNodeToDrawConnectionLineFrom(RoomNodeSO roomNode, Vector2 position)
        {
            roomNodeToDrawLineFrom = roomNode;
            linePosition = position;
        }
#endif

        #endregion
    }
}
