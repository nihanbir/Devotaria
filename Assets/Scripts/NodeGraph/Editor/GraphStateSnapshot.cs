using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph.Editor
{
    /// <summary>
    /// Represents a snapshot of the graph state for undo operations
    /// </summary>
    [Serializable]
    public class GraphStateSnapshot
    {
        public List<NodeSnapshot> nodes;
        public List<ConnectionSnapshot> connections;
        public bool hasConnectedBossRoom;

        [Serializable]
        public class NodeSnapshot
        {
            public string id;
            public Rect rect;
            public string roomNodeTypeId;
            public List<string> parentRoomNodeIDList;
            public List<string> childRoomNodeIDList;
            public float nodeWidth;
            public float nodeHeight;
        }

        [Serializable]
        public class ConnectionSnapshot
        {
            public string parentId;
            public string childId;
        }

        public GraphStateSnapshot(RoomNodeGraphSO graph)
        {
            nodes = new List<NodeSnapshot>();
            connections = new List<ConnectionSnapshot>();
            hasConnectedBossRoom = graph.hasConnectedBossRoom;

            foreach (var node in graph.roomNodeList)
            {
                var nodeSnapshot = new NodeSnapshot
                {
                    id = node.id,
                    rect = node.rect,
                    roomNodeTypeId = node.roomNodeType ? node.roomNodeType.name : null,
                    parentRoomNodeIDList = new List<string>(node.parentRoomNodeIDList),
                    childRoomNodeIDList = new List<string>(node.childRoomNodeIDList),
                    nodeWidth = node.rect.width,
                    nodeHeight = node.rect.height
                };
                nodes.Add(nodeSnapshot);

                // Store connections
                foreach (var childId in node.childRoomNodeIDList)
                {
                    connections.Add(new ConnectionSnapshot
                    {
                        parentId = node.id,
                        childId = childId
                    });
                }
            }
        }
    }
}