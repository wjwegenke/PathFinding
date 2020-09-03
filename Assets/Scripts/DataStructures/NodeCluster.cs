using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NodeCluster
{
    public int xIndex;
    public int yIndex;
    public int zIndex;

    public List<DyNode> dyNodes;

    public NodeCluster(int x, int y, int z) {
        xIndex = x;
        yIndex = y;
        zIndex = z;
        dyNodes = new List<DyNode>();
    }

    public void AddDyNode(DyNode dyNode) {
        dyNode.nodeCluster = this;
        if (!dyNodes.Contains(dyNode))
            dyNodes.Add(dyNode);
    }

    public void RemoveDyNode(DyNode dyNode) {
        dyNode.nodeCluster = null;
        if (dyNodes.Contains(dyNode))
            dyNodes.Remove(dyNode);
    }

    public void UpdateNodes() {
        dyNodes.ToList().ForEach(dyNode => dyNode.UpdateSelf());
    }
}
