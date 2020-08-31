using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IHeapItem<Node>
{
    private int heapIndex;
    public Enums.NodeState state;
    public bool canWalkOn = false;
    public List<Node> neighbors = new List<Node>();
    public Vector3 castPoint;
    public Vector3 worldPosition;
    private Vector3 worldNormal;
    public float slope;
    public int gridY;
    public int gridX;
    public int gridZ;
    public int movementPenalty;
    public int blurredPenalty;
    public NodeGrid nodeGrid;

    public int gCost;
    public int hCost;
    public Node parent;
    public int FCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public int HeapIndex
    {
        get { return heapIndex; }
        set { heapIndex = value; }
    }

    public Node(int gridX, int gridZ, int gridY, Vector3 castPoint, NodeGrid nodeGrid)
    {
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.gridY = gridY;
        this.castPoint = castPoint;
        this.nodeGrid = nodeGrid;
        CalculateGround();
    }

    public void CalculateGround() {
        Ray ray = new Ray(castPoint, Vector3.down);
        RaycastHit hit;
        float nodeRadius = nodeGrid.nodeSpacing / 2;
        float sphereRadius = nodeRadius / 4;
        Collider[] hitColliders = new Collider[1];
        if (Physics.OverlapSphereNonAlloc(castPoint, 0, hitColliders, nodeGrid.movementMask) > 0) {
            worldPosition = castPoint;
            worldNormal = Vector3.up;
            slope = 0f;
            state = Enums.NodeState.Wall;
            if (!nodeGrid.walkableRegionsDictionary.TryGetValue(hitColliders[0].transform.gameObject.layer, out movementPenalty))
                nodeGrid.unwalkableRegionsDictionary.TryGetValue(hitColliders[0].transform.gameObject.layer, out movementPenalty);
        }
        else if (Physics.Raycast(ray, out hit, nodeGrid.nodeSpacing, nodeGrid.movementMask))
        {
            if (nodeGrid.walkableRegionsDictionary.TryGetValue(hit.transform.gameObject.layer, out movementPenalty))
                state = Enums.NodeState.Walkable;
            else if (nodeGrid.unwalkableRegionsDictionary.TryGetValue(hit.transform.gameObject.layer, out movementPenalty))
                state = Enums.NodeState.Unwalkable;
            
            worldPosition = castPoint + Vector3.down * hit.distance;
            worldNormal = hit.normal;
            slope = Mathf.Acos(hit.normal.y);
        }
        else
        {
            movementPenalty = 0;
            worldPosition = castPoint;
            worldNormal = Vector3.up;
            slope = 0f;
            state = Enums.NodeState.Air;
        }
    }

    public void CalculateBlurredPenalty(int kernelSize) {
        int kernelExtent = kernelSize / 2;
        int blurValue = 0;

        int count = 0;
        for (int x = gridX - kernelExtent; x <= gridX + kernelExtent; x++) {
            if (x < 0 || x >= nodeGrid.gridSizeX) continue;
            for (int z = gridZ - kernelExtent; z <= gridZ + kernelExtent; z++) {
                if (z < 0 || z >= nodeGrid.gridSizeZ) continue;
                for (int y = gridY - kernelExtent; y <= gridY + kernelExtent; y++) {
                    if (y < 0 || y >= nodeGrid.gridSizeY
                        || nodeGrid.nodeGrid[x,z,y].state == Enums.NodeState.Air) continue;

                    blurValue += nodeGrid.nodeGrid[x,z,y].movementPenalty;
                    count++;
                }
            }
        }
        if (count != 0)
            blurredPenalty = blurValue / count;
        else
            blurredPenalty = movementPenalty;
    }

    private bool CheckDefaultValues(Vector3 size, float stepSize, float maxSlope) {
        bool sizeEqual = (size - nodeGrid.defaultCharacterSize).sqrMagnitude < 0.001f;
        bool stepSizeEqual = Mathf.Abs(stepSize - nodeGrid.defaultStepSize) < 0.01f;
        bool maxSlopeEqual = Mathf.Abs(maxSlope - nodeGrid.defaultMaxSlope) < 0.01f;
        return sizeEqual && stepSizeEqual && maxSlopeEqual;
    }

    public void CalculateCanWalkOn(Vector3 size, float stepSize, float maxSlope) {
        canWalkOn = CanWalkOn(size, stepSize, maxSlope, true);
    }

    public void CalculateNeighbors(Vector3 size, float stepSize, float maxSlope) {
        neighbors = GetNeighbours(size, stepSize, maxSlope, true);
    }

    public bool CanWalkOn(Vector3 size, float stepSize, float maxSlope, bool overwriteDefault = false) {
        if (!overwriteDefault && CheckDefaultValues(size, stepSize, maxSlope))
            return canWalkOn;

        if (state != Enums.NodeState.Walkable || slope > maxSlope)
            return false;
            
        Bounds targetBounds = new Bounds(worldPosition + Vector3.up * size.y / 2, size);
        List<Node> nodes = nodeGrid.GetNodesWithin(targetBounds.min, targetBounds.max);
        foreach(Node n in nodes) {
            if (n.state == Enums.NodeState.Unwalkable)
                return false;
            
            if (n.state == Enums.NodeState.Wall) {
                if (Mathf.Abs(n.worldPosition.y - worldPosition.y) < stepSize) //Small enough to step over
                    continue;
                else
                    return false;
            }


            if (n.state == Enums.NodeState.Walkable) {
                if (Mathf.Abs(n.worldPosition.y - worldPosition.y) <= stepSize) //Small enough to step onto
                    continue;
                else
                    return false;
            }
        }
        return true;
    }

    public List<Node> GetNeighbours(Vector3 size, float stepSize, float maxSlope, bool overwriteDefault = false) {
        if (!overwriteDefault && CheckDefaultValues(size, stepSize, maxSlope))
            return neighbors;

        List<Node> neighbours = new List<Node>();
        
        int startX = Mathf.Clamp(gridX - 1, 0, nodeGrid.gridSizeX - 1);
        int startZ = Mathf.Clamp(gridZ - 1, 0, nodeGrid.gridSizeZ - 1);
        int startY = Mathf.Clamp(gridY - 1, 0, nodeGrid.gridSizeY - 1);
        
        int endX = Mathf.Clamp(gridX + 1, 0, nodeGrid.gridSizeX - 1);
        int endZ = Mathf.Clamp(gridZ + 1, 0, nodeGrid.gridSizeZ - 1);
        int endY = Mathf.Clamp(gridY + 1, 0, nodeGrid.gridSizeY - 1);

        for (int x = startX; x <= endX; x++)
        {
            for (int z = startZ; z <= endZ; z++)
            {
                if (x == gridX && z == gridZ)
                    continue;

                for (int y = startY; y <= endY; y++)
                {
                    Node node = nodeGrid.nodeGrid[x,z,y];
                    if (Mathf.Abs(node.worldPosition.y - worldPosition.y) < stepSize && node.CanWalkOn(size, stepSize, maxSlope))
                        neighbours.Add(node);
                }
            }
        }
        return neighbours;
    }

    public int CompareTo(Node nodeToCompare)
    {
        int compare = FCost.CompareTo(nodeToCompare.FCost);
        if (compare == 0)
            compare = hCost.CompareTo(nodeToCompare.hCost);

        return -compare;
    }
}