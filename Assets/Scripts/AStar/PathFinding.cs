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
        HashSet<Node> acceptableNodes = new HashSet<Node>();

        if (targetNode.CanWalkOn(request.characterSize, request.stepSize, request.maxSlope)) {
            Heap<Node> openSet = new Heap<Node>(nodeGrid.MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();

            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                //currentNode == targetNode
                if ((currentNode.worldPosition - targetNode.worldPosition).sqrMagnitude <= request.radius * request.radius)
                    //&& (!request.lineOfSight || Helper.HasClearLineOfSight(currentNode.worldPosition + Vector3.up * request.heightOffGround, request.visualTarget)))
                {
                    targetNode = currentNode;
                    pathSuccess = true;
                    break;
                }

                foreach (Node neighbour in currentNode.GetNeighbours(request.characterSize, request.stepSize, request.maxSlope))
                {
                    if (closedSet.Contains(neighbour))
                        continue;

                    int newCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.blurredPenalty;
                    //int newCostToNeighbour = currentNode.gCost + Mathf.RoundToInt(Vector3.Distance(currentNode.worldPosition, neighbour.worldPosition) * 100) + neighbour.blurredPenalty * 100;
                    if (newCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newCostToNeighbour;
                        neighbour.hCost =  GetDistance(neighbour, targetNode);
                        //neighbour.hCost =  Mathf.RoundToInt(Vector3.Distance(neighbour.worldPosition, targetNode.worldPosition) * 100);
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                        else
                            openSet.UpdateItem(neighbour);
                    }
                }
            }
        }

        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
            pathSuccess = waypoints.Length > 0;
        }
        callback(new PathResult(waypoints, pathSuccess, request.callback));
    }

    private Vector3[] RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
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
