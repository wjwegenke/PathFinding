using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeCost : IHeapItem<NodeCost>
{
    private int heapIndex;
    public Node node;
    public NodeCost parent;
    public int gCost;
    public int hCost;
    public int FCost {
        get { return gCost + hCost; }
    }

    public int HeapIndex
    {
        get { return heapIndex; }
        set { heapIndex = value; }
    }

    public NodeCost(Node _node) {
        this.node = _node;
        gCost = 0;
        hCost = 0;
        parent = null;
    }
    public NodeCost(Node _node, NodeCost _parent) {
        this.node = _node;
        this.gCost = 0;
        this.hCost = 0;
        this.parent = _parent;
    }
    public NodeCost(Node _node, NodeCost _parent, int _gCost, int _hCost) {
        this.node = _node;
        this.gCost = _gCost;
        this.hCost = _hCost;
        this.parent = _parent;
    }

    public int CompareTo(NodeCost nodeCostToCompare)
    {
        int compare = FCost.CompareTo(nodeCostToCompare.FCost);
        if (compare == 0)
            compare = hCost.CompareTo(nodeCostToCompare.hCost);

        return -compare;
    }
}
