using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph.Editor
{
    /// <summary>
    /// Represents different types of actions that can be undone
    /// </summary>
    public enum UndoActionType
    {
        AddNode,
        DeleteNode,
        AddConnection,
        DeleteConnection,
        ChangeNodeType
    }

    /// <summary>
    /// Stores information needed to undo a graph modification
    /// </summary>
    [Serializable]
    public class UndoAction
    {
        public UndoActionType actionType;
        
        // For node operations
        public string nodeId;
        public Vector2 nodePosition;
        public RoomNodeTypeSO nodeType;
        public float nodeWidth;
        public float nodeHeight;
        
        // For connection operations
        public string parentNodeId;
        public string childNodeId;
        
        // For node type changes
        public RoomNodeTypeSO previousNodeType;
        public RoomNodeTypeSO newNodeType;
        
        // For delete operations - store child/parent connections
        public List<string> childConnections = new List<string>();
        public List<string> parentConnections = new List<string>();
        
        public UndoAction(UndoActionType type)
        {
            actionType = type;
        }
    }
}