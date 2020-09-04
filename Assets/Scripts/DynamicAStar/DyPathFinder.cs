using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DyPathFinder
{
    public void FindPath(DyPathRequest request, Action<DyPathResult> callback) {
        DyNode[] path = new DyNode[0];
        bool pathSuccess = false;
        DyNode startDyNode = DyNodeManager.GetDyNodeFromWorldPosition(request.pathStart);
        DyNode endDyNode = DyNodeManager.GetDyNodeFromWorldPosition(request.pathEnd);
        DyNodeCost startDyNodeCost = new DyNodeCost(startDyNode);
        DyNodeCost endDyNodeCost = new DyNodeCost(endDyNode);

        if (startDyNode != null && endDyNode != null && endDyNode.walkable) {
            Dictionary<DyNode, DyNodeCost> openSetDyNodeCostDict = new Dictionary<DyNode, DyNodeCost>(DyNodeManager.Instance.dyNodeCount);
            Heap<DyNodeCost> openSetCost = new Heap<DyNodeCost>(DyNodeManager.Instance.dyNodeCount);
            HashSet<DyNode> closedSet = new HashSet<DyNode>();

            openSetCost.Add(startDyNodeCost);
            openSetDyNodeCostDict.Add(startDyNodeCost.dyNode, startDyNodeCost);

            while (openSetCost.Count > 0) {
                DyNodeCost currentDyNodeCost = openSetCost.RemoveFirst();
                openSetDyNodeCostDict.Remove(currentDyNodeCost.dyNode);
                closedSet.Add(currentDyNodeCost.dyNode);

                if (currentDyNodeCost.dyNode == endDyNodeCost.dyNode) {
                    endDyNodeCost = currentDyNodeCost;
                    pathSuccess = true;
                    break;
                }

                foreach (DyNode neighbour in currentDyNodeCost.dyNode.neighbours) {
                    if (closedSet.Contains(neighbour) || !neighbour.walkable) continue;

                    DyNodeCost neighbourDyNodeCost;
                    if (!openSetDyNodeCostDict.TryGetValue(neighbour, out neighbourDyNodeCost))
                        neighbourDyNodeCost = new DyNodeCost(neighbour);

                    float newCostToNeighbour = currentDyNodeCost.gCost + Vector3.Distance(currentDyNodeCost.dyNode.worldPosition, neighbourDyNodeCost.dyNode.worldPosition) + neighbourDyNodeCost.dyNode.blurredPenalty;
                    if (newCostToNeighbour < neighbourDyNodeCost.gCost || !openSetDyNodeCostDict.ContainsKey(neighbourDyNodeCost.dyNode)) {
                        neighbourDyNodeCost.gCost = newCostToNeighbour;
                        neighbourDyNodeCost.parent = currentDyNodeCost;

                        if (!openSetDyNodeCostDict.ContainsKey(neighbourDyNodeCost.dyNode)) {
                            neighbourDyNodeCost.hCost = Vector3.Distance(endDyNode.worldPosition, neighbourDyNodeCost.dyNode.worldPosition);
                            openSetDyNodeCostDict.Add(neighbourDyNodeCost.dyNode, neighbourDyNodeCost);
                            openSetCost.Add(neighbourDyNodeCost);
                        } else {
                            openSetCost.UpdateItem(neighbourDyNodeCost);
                        }
                    }
                }
            }
        }

        if (pathSuccess) {
            path = RetracePath(startDyNodeCost, endDyNodeCost);
            pathSuccess = path.Length > 0;
        }
        callback(new DyPathResult(path, pathSuccess, request.callback));
    }

    private DyNode[] RetracePath(DyNodeCost startDyNodeCost, DyNodeCost endDyNodeCost) {
        List<DyNode> path = new List<DyNode>();
        DyNodeCost currentDyNodeCost = endDyNodeCost;

        while (currentDyNodeCost.parent != null)
        {
            path.Add(currentDyNodeCost.dyNode);
            currentDyNodeCost = currentDyNodeCost.parent;
        }

        path.Reverse();
        return path.ToArray();
    }
}
