using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DyNodeCost : IHeapItem<DyNodeCost>
{
    private int heapIndex;
    public DyNode dyNode;
    public DyNodeCost parent;
    public float gCost;
    public float hCost;
    public float FCost {
        get { return gCost + hCost; }
    }

    public int HeapIndex
    {
        get { return heapIndex; }
        set { heapIndex = value; }
    }

    public DyNodeCost(DyNode _dyNode) {
        this.dyNode = _dyNode;
        gCost = 0;
        hCost = 0;
        parent = null;
    }
    public DyNodeCost(DyNode _dyNode, DyNodeCost _parent) {
        this.dyNode = _dyNode;
        this.gCost = 0;
        this.hCost = 0;
        this.parent = _parent;
    }
    public DyNodeCost(DyNode _dyNode, DyNodeCost _parent, int _gCost, int _hCost) {
        this.dyNode = _dyNode;
        this.gCost = _gCost;
        this.hCost = _hCost;
        this.parent = _parent;
    }

    public int CompareTo(DyNodeCost dyNodeCostToCompare)
    {
        int compare = FCost.CompareTo(dyNodeCostToCompare.FCost);
        if (compare == 0)
            compare = hCost.CompareTo(dyNodeCostToCompare.hCost);

        return -compare;
    }
}
