using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PathFinding : MonoBehaviour
{
    private NodeGrid nodeGrid;
    void Awake()
    {
        nodeGrid = GetComponent<NodeGrid>();
    }

    public void FindPath(PathRequest request, Action<PathResult> callback) {
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;
        Node startNode = nodeGrid.GetNodeFromWorldPoint(request.pathStart);
        Node targetNode = nodeGrid.GetNodeFromWorldPoint(request.pathEnd);
        NodeCost startNodeCost = new NodeCost(startNode);
        NodeCost targetNodeCost = new NodeCost(targetNode);
        HashSet<Node> acceptableNodes = new HashSet<Node>();

        if (targetNode.CanWalkOn(request.characterSize, request.stepSize, request.maxSlope)) {
            Heap<NodeCost> openSetCost = new Heap<NodeCost>(nodeGrid.MaxSize);
            Dictionary<Node, NodeCost> nodeCostDict = new Dictionary<Node, NodeCost>(nodeGrid.MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSetCost.Add(startNodeCost);
            nodeCostDict.Add(startNodeCost.node, startNodeCost);

            while (openSetCost.Count > 0)
            {
                NodeCost currentNodeCost = openSetCost.RemoveFirst();
                nodeCostDict.Remove(currentNodeCost.node);
                closedSet.Add(currentNodeCost.node);

                //currentNode == targetNode
                if ((currentNodeCost.node.worldPosition - targetNodeCost.node.worldPosition).sqrMagnitude <= request.radius * request.radius)
                    //&& (!request.lineOfSight || Helper.HasClearLineOfSight(currentNode.worldPosition + Vector3.up * request.heightOffGround, request.visualTarget)))
                {
                    targetNodeCost = currentNodeCost;
                    pathSuccess = true;
                    break;
                }

                foreach (Node neighbour in currentNodeCost.node.GetNeighbours(request.characterSize, request.stepSize, request.maxSlope))
                {
                    if (closedSet.Contains(neighbour))
                        continue;
                    NodeCost neighbourCost;
                    if (!nodeCostDict.TryGetValue(neighbour, out neighbourCost)) {
                        neighbourCost = new NodeCost(neighbour);
                    }

                    int newCostToNeighbour = currentNodeCost.gCost + GetDistance(currentNodeCost.node, neighbourCost.node) + neighbourCost.node.blurredPenalty;
                    //int newCostToNeighbour = currentNode.gCost + Mathf.RoundToInt(Vector3.Distance(currentNode.worldPosition, neighbour.worldPosition) * 100) + neighbour.blurredPenalty * 100;
                    if (newCostToNeighbour < neighbourCost.gCost || !openSetCost.Contains(neighbourCost))
                    {
                        neighbourCost.gCost = newCostToNeighbour;
                        neighbourCost.hCost =  GetDistance(neighbourCost.node, targetNode);
                        //neighbour.hCost =  Mathf.RoundToInt(Vector3.Distance(neighbour.worldPosition, targetNode.worldPosition) * 100);
                        neighbourCost.parent = currentNodeCost;

                        if (!openSetCost.Contains(neighbourCost)) {
                            openSetCost.Add(neighbourCost);
                            nodeCostDict.Add(neighbourCost.node, neighbourCost);
                        } else {
                            openSetCost.UpdateItem(neighbourCost);
                        }
                    }
                }
            }
        }

        if (pathSuccess)
        {
            waypoints = RetracePath(startNodeCost, targetNodeCost);
            pathSuccess = waypoints.Length > 0;
            // if (pathSuccess && goToExactLocation) {
            //     waypoints[waypoints.Length - 1] = request.pathEnd;
            // }
        }
        callback(new PathResult(waypoints, pathSuccess, request.callback));
    }

    private Vector3[] RetracePath(NodeCost startNodeCost, NodeCost endNodeCost)
    {
        List<Node> path = new List<Node>();
        NodeCost currentNode = endNodeCost;

        while (currentNode != startNodeCost)
        {
            path.Add(currentNode.node);
            currentNode = currentNode.parent;
        }

        Vector3[] waypoints = SimplifyPath(path);
        Array.Reverse(waypoints);
        return waypoints;
    }

    //change this to include height (Y)
    Vector3[] SimplifyPath(List<Node> path)
    {
        List<Vector3> waypoints = new List<Vector3>();

        Vector3 directionOld = Vector3.zero;
        
        int i;
        for(i = 0; i < path.Count - 1; i++)
        {
            Vector3 directionNew = path[i+1].worldPosition - path[i].worldPosition;
            directionNew.Normalize();
            if (directionNew != directionOld)
            {
                waypoints.Add(path[i].worldPosition);
            }
            directionOld = directionNew;
        }
        if (path.Count > i)
            waypoints.Add(path[i].worldPosition);
        return waypoints.ToArray();
    }

    private int GetDistance(Node nodeA, Node nodeB)
    {
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (distX > distZ)
            return 14 * distZ + 10 * (distX - distZ) + 20 * distY;
        return 14 * distX + 10 * (distZ - distX) + 20 * distY;
    }
}
