using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Object;

namespace NodeGraph.Editor
{
    public class GraphUndoActionManager
    {
        // Undo system - stack to store the last 3 actions
        public Stack<UndoAction> UndoStack = new Stack<UndoAction>();
        public readonly int MaxUndoActions = 3;
        public RoomNodeGraphSO RoomNodeGraph;
        
         /// <summary>
        /// Records an add node action for undo
        /// </summary>
        public void RecordAddNodeAction(RoomNodeSO node)
        {
            UndoAction action = new UndoAction(UndoActionType.AddNode)
            {
                nodeId = node.id,
                nodePosition = node.rect.position,
                nodeWidth = node.rect.width,
                nodeHeight = node.rect.height,
                nodeType = node.roomNodeType
            };
            
            PushUndoAction(action);
        }
        
        /// <summary>
        /// Records a delete node action for undo
        /// </summary>
        public void RecordDeleteNodeAction(RoomNodeSO node)
        {
            UndoAction action = new UndoAction(UndoActionType.DeleteNode)
            {
                nodeId = node.id,
                nodePosition = node.rect.position,
                nodeWidth = node.rect.width,
                nodeHeight = node.rect.height,
                nodeType = node.roomNodeType,
                childConnections = new List<string>(node.childRoomNodeIDList),
                parentConnections = new List<string>(node.parentRoomNodeIDList)
            };
            
            PushUndoAction(action);
        }
        
        /// <summary>
        /// Records an add connection action for undo
        /// </summary>
        public void RecordAddConnectionAction(string parentId, string childId)
        {
            UndoAction action = new UndoAction(UndoActionType.AddConnection)
            {
                parentNodeId = parentId,
                childNodeId = childId
            };
            
            PushUndoAction(action);
        }
        
        /// <summary>
        /// Records a delete connection action for undo
        /// </summary>
        public void RecordDeleteConnectionAction(string parentId, string childId)
        {
            UndoAction action = new UndoAction(UndoActionType.DeleteConnection)
            {
                parentNodeId = parentId,
                childNodeId = childId
            };
            
            PushUndoAction(action);
        }
        
        /// <summary>
        /// Records a node type change action for undo
        /// </summary>
        public void RecordChangeNodeTypeAction(RoomNodeSO node, RoomNodeTypeSO previousType, RoomNodeTypeSO newType)
        {
            UndoAction action = new UndoAction(UndoActionType.ChangeNodeType)
            {
                nodeId = node.id,
                previousNodeType = previousType,
                newNodeType = newType
            };
            
            PushUndoAction(action);
        }
        
        /// <summary>
        /// Pushes an action to the undo stack, maintaining max size of 3
        /// </summary>
        public void PushUndoAction(UndoAction action)
        {
            if (UndoStack.Count >= MaxUndoActions)
            {
                // Convert to array, keep most recent items
                var items = UndoStack.ToArray();
                UndoStack.Clear();
        
                // Add back the most recent items (skip the oldest)
                for (int i = MaxUndoActions - 2; i >= 0; i--)
                {
                    UndoStack.Push(items[i]);
                }
            }
            UndoStack.Push(action);
        }
        
        /// <summary>
        /// Performs the undo operation for the last action
        /// </summary>
        public void PerformUndo()
        {
            if (UndoStack.Count == 0) return;
            
            if (!RoomNodeGraph) return;
            
            UndoAction action = UndoStack.Pop();
            
            switch (action.actionType)
            {
                case UndoActionType.AddNode:
                    UndoAddNode(action);
                    break;
                    
                case UndoActionType.DeleteNode:
                    UndoDeleteNode(action);
                    break;
                    
                case UndoActionType.AddConnection:
                    UndoAddConnection(action);
                    break;
                    
                case UndoActionType.DeleteConnection:
                    UndoDeleteConnection(action);
                    break;
                    
                case UndoActionType.ChangeNodeType:
                    UndoChangeNodeType(action);
                    break;
            }
            
            AssetDatabase.SaveAssets();
            GUI.changed = true;
        }
        
        /// <summary>
        /// Undoes an add node action by deleting the node
        /// </summary>
        public void UndoAddNode(UndoAction action)
        {
            RoomNodeSO node = RoomNodeGraph.GetRoomNode(action.nodeId);
            if (!node) return;
            
            // Don't record this deletion as a new undo action
            node.IsSelected = false;
            node.RemoveAllNodeConnections();
            RoomNodeGraph.RoomNodeDictionary.Remove(node.id);
            RoomNodeGraph.roomNodeList.Remove(node);
            DestroyImmediate(node, true);
        }
        
        /// <summary>
        /// Undoes a delete node action by recreating the node
        /// </summary>
        public void UndoDeleteNode(UndoAction action)
        {
            RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();
            
            RoomNodeGraph.roomNodeList.Add(roomNode);
            
            // Restore the original ID and properties
            roomNode.Initialize(new Rect(action.nodePosition, new Vector2(action.nodeWidth, action.nodeHeight)), 
                RoomNodeGraph, action.nodeType);
            roomNode.id = action.nodeId; // Override with original ID
            
            AssetDatabase.AddObjectToAsset(roomNode, RoomNodeGraph);
            
            // Repopulate the dictionary before restoring connections
            RoomNodeGraph.OnValidate();
            
            // Restore connections
            foreach (string parentId in action.parentConnections)
            {
                RoomNodeSO parentNode = RoomNodeGraph.GetRoomNode(parentId);
                if (parentNode)
                {
                    parentNode.AddChildRoomNodeConnection(roomNode.id);
                }
            }
            
            foreach (string childId in action.childConnections)
            {
                roomNode.AddChildRoomNodeConnection(childId);
            }
            
        }
        
        /// <summary>
        /// Undoes an add connection action by removing the connection
        /// </summary>
        public void UndoAddConnection(UndoAction action)
        {
            RoomNodeSO parentNode = RoomNodeGraph.GetRoomNode(action.parentNodeId);
            if (!parentNode) return;
            
            parentNode.RemoveChildRoomNodeConnection(action.childNodeId);
        }
        
        /// <summary>
        /// Undoes a delete connection action by recreating the connection
        /// </summary>
        public void UndoDeleteConnection(UndoAction action)
        {
            RoomNodeSO parentNode = RoomNodeGraph.GetRoomNode(action.parentNodeId);
            RoomNodeSO childNode = RoomNodeGraph.GetRoomNode(action.childNodeId);
            
            if (parentNode && childNode)
            {
                parentNode.AddChildRoomNodeConnection(action.childNodeId);
            }
        }
        
        /// <summary>
        /// Undoes a node type change by restoring the previous type
        /// </summary>
        public void UndoChangeNodeType(UndoAction action)
        {
            RoomNodeSO node = RoomNodeGraph.GetRoomNode(action.nodeId);
            if (!node) return;
            
            node.roomNodeType = action.previousNodeType;
            node.name = action.previousNodeType.roomNodeTypeName;
            EditorUtility.SetDirty(node);
        }
    }
}