using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeGrid : MonoBehaviour
{
    public bool displayGizmos = true;
    public bool displayGizmosAir = false;
    public bool displayGizmosUnwalkable = false;
    public bool displayGizmosWalkable = false;
    public bool displayGizmosWall = false;
    private int minBlurredPenalty = int.MaxValue;
    private int maxBlurredPenalty = int.MinValue;

    public Vector3 gridWorldSize;
    public float nodeSpacing = 1f;
    public float blurSize = 1f;
    public TerrainType[] walkableRegions;
    private LayerMask walkableMask;
    public Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    public TerrainType[] unwalkableRegions;
    private LayerMask unwalkableMask;
    public Dictionary<int, int> unwalkableRegionsDictionary = new Dictionary<int, int>();
    private Vector3 worldBottomLeft;
    
    public Vector3 defaultCharacterSize = new Vector3(1,1.7f,1);
    public float defaultStepSize = 0.75f;
    public float defaultMaxSlope = 0.875f;

    private int KernelSize {
        get { return Mathf.RoundToInt(blurSize / nodeSpacing) * 2 + 1; }
    }
    
    
    [HideInInspector]
    public LayerMask movementMask;
    [HideInInspector]
    public int gridSizeX, gridSizeY, gridSizeZ;
    public Node[,,] nodeGrid;

    public int MaxSize
    {
        get { return gridSizeX * gridSizeZ * gridSizeY; }
    }
    

    void Awake()
    {
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeSpacing);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeSpacing);
        gridSizeZ = Mathf.RoundToInt(gridWorldSize.z / nodeSpacing);

        foreach (TerrainType region in walkableRegions)
        {
            walkableMask.value |= region.terrainMask.value;
            walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
        }
        foreach (TerrainType region in unwalkableRegions)
        {
            unwalkableMask.value |= region.terrainMask.value;
            unwalkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
        }
        movementMask.value = walkableMask.value | unwalkableMask.value;
    }

    private void Start() {
        CreateGrid();
    }

    private void CreateGrid()
    {
        nodeGrid = new Node[gridSizeX, gridSizeZ, gridSizeY];
        worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.z / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    float halfSpacing = nodeSpacing / 2f;
                    Vector3 castPoint = worldBottomLeft + Vector3.right * (x * nodeSpacing + halfSpacing) + Vector3.forward * (z * nodeSpacing + halfSpacing) + Vector3.up * (y * nodeSpacing + nodeSpacing);
                    nodeGrid[x, z, y] = new Node(x, z, y, castPoint, this);
                }
            }
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    if (nodeGrid[x, z, y].state != Enums.NodeState.Air)
                        nodeGrid[x, z, y].CalculateBlurredPenalty(KernelSize);

                    nodeGrid[x, z, y].CalculateCanWalkOn(defaultCharacterSize, defaultStepSize, defaultMaxSlope);
                    nodeGrid[x, z, y].CalculateNeighbors(defaultCharacterSize, defaultStepSize, defaultMaxSlope);
                }
            }
        }

        //BlurPenalties(Mathf.RoundToInt(blurSize / nodeSpacing));
    }

    public void RecalculateNodes(Vector3 min, Vector3 max) {
        int kernelExtents = (KernelSize - 1) / 2;
        min = min - Vector3.one * nodeSpacing * kernelExtents;
        max = max + Vector3.one * nodeSpacing * kernelExtents;
        var nodes = GetNodesWithin(min, max);
        foreach (Node node in nodes) {
            node.CalculateGround();
        }
        foreach (Node node in nodes) {
            node.CalculateBlurredPenalty(KernelSize);
            node.CalculateCanWalkOn(defaultCharacterSize, defaultStepSize, defaultMaxSlope);
            node.CalculateNeighbors(defaultCharacterSize, defaultStepSize, defaultMaxSlope);

            if (node.blurredPenalty > 200)
                Debug.Log(node.blurredPenalty);

            if (node.blurredPenalty > maxBlurredPenalty)
                maxBlurredPenalty = node.blurredPenalty;
            if (node.blurredPenalty < minBlurredPenalty)
                minBlurredPenalty = node.blurredPenalty;
        }
    }

    public void BlurPenalties(int blurSize) {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        int[,,] penaltiesXPass = new int[gridSizeX, gridSizeZ, gridSizeY];
        int[,,] penaltiesZPass = new int[gridSizeX, gridSizeZ, gridSizeY];
        int[,,] penaltiesYPass = new int[gridSizeX, gridSizeZ, gridSizeY];

        //X Pass
        for (int y = 0; y < gridSizeY; y++) {
            for (int z = 0; z < gridSizeZ; z++) {
                for (int x = -kernelExtents; x <= kernelExtents; x++) {
                    int sampleX = Mathf.Clamp(x, 0, gridSizeX - 1);
                    penaltiesXPass[0,z,y] += nodeGrid[sampleX,z,y].movementPenalty;
                }
                for (int x = 1; x < gridSizeX; x++) {
                    int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX - 1);
                    int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);
                    penaltiesXPass[x,z,y] = penaltiesXPass[x-1,z,y] - nodeGrid[removeIndex,z,y].movementPenalty + nodeGrid[addIndex,z,y].movementPenalty;
                }
            }
        }

        //Z Pass
        for (int y = 0; y < gridSizeY; y++) {
            for (int x = 0; x < gridSizeX; x++) {
                for (int z = -kernelExtents; z <= kernelExtents; z++) {
                    int sampleZ = Mathf.Clamp(z, 0, gridSizeZ - 1);
                    penaltiesZPass[x,0,y] += penaltiesXPass[x,sampleZ,y];
                }
                for (int z = 1; z < gridSizeZ; z++) {
                    int removeIndex = Mathf.Clamp(z - kernelExtents - 1, 0, gridSizeZ - 1);
                    int addIndex = Mathf.Clamp(z + kernelExtents, 0, gridSizeZ - 1);
                    penaltiesZPass[x,z,y] = penaltiesZPass[x,z-1,y] - penaltiesXPass[x,removeIndex,y] + penaltiesXPass[x,addIndex,y];
                }
            }
        }

        //Y Pass
        for (int x = 0; x < gridSizeX; x++) {
            for (int z = 0; z < gridSizeZ; z++) {
                for (int y = -kernelExtents; y <= kernelExtents; y++) {
                    int sampleY = Mathf.Clamp(z, 0, gridSizeY - 1);
                    penaltiesYPass[x,z,0] += penaltiesZPass[x,z,sampleY];
                }

                int blurredPenalty = Mathf.RoundToInt((float)penaltiesYPass[x,z,0] / (kernelSize * kernelSize * kernelSize));
                nodeGrid[x,z,0].blurredPenalty = blurredPenalty;

                if (blurredPenalty > maxBlurredPenalty)
                    maxBlurredPenalty = blurredPenalty;
                if (blurredPenalty < minBlurredPenalty)
                    minBlurredPenalty = blurredPenalty;

                for (int y = 1; y < gridSizeY; y++) {
                    int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY - 1);
                    int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);
                    penaltiesYPass[x,z,y] = penaltiesYPass[x,z,y-1] - penaltiesZPass[x,z,removeIndex] + penaltiesZPass[x,z,addIndex];

                    blurredPenalty = Mathf.RoundToInt((float)penaltiesYPass[x,z,y] / (kernelSize * kernelSize * kernelSize));
                    nodeGrid[x,z,y].blurredPenalty = blurredPenalty;

                    if (blurredPenalty > maxBlurredPenalty)
                        maxBlurredPenalty = blurredPenalty;
                    if (blurredPenalty < minBlurredPenalty)
                        minBlurredPenalty = blurredPenalty;
                }
            }
        }
    }

    /*
    private struct PenaltyStruct {
        public int value;
        public int count;
    }

    public void BlurPenalties(int blurSize) {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        PenaltyStruct[,,] penaltiesXPass = new PenaltyStruct[gridSizeX, gridSizeZ, gridSizeY];
        PenaltyStruct[,,] penaltiesZPass = new PenaltyStruct[gridSizeX, gridSizeZ, gridSizeY];
        PenaltyStruct[,,] penaltiesYPass = new PenaltyStruct[gridSizeX, gridSizeZ, gridSizeY];

        //X Pass
        for (int y = 0; y < gridSizeY; y++) {
            for (int z = 0; z < gridSizeZ; z++) {
                for (int x = 0; x <= kernelExtents && x < gridSizeX; x++) {
                    if (nodeGrid[x,z,y].state == Enums.NodeState.Air)
                        continue;

                    penaltiesXPass[0,z,y].value += nodeGrid[x,z,y].movementPenalty;
                    penaltiesXPass[0,z,y].count++;
                }
                for (int x = 1; x < gridSizeX; x++) {
                    penaltiesXPass[x,z,y].value = penaltiesXPass[x-1,z,y].value;
                    penaltiesXPass[x,z,y].count = penaltiesXPass[x-1,z,y].count;
                    int removeIndex = x - kernelExtents;
                    int addIndex = x + kernelExtents;
                    if (removeIndex >= 0) {
                        penaltiesXPass[removeIndex,z,y].value -= nodeGrid[removeIndex,z,y].movementPenalty;
                        if (nodeGrid[removeIndex,z,y].state != Enums.NodeState.Air)
                            penaltiesXPass[removeIndex,z,y].count--;
                    }
                    if (addIndex < gridSizeX) {
                        penaltiesXPass[addIndex,z,y].value += nodeGrid[addIndex,z,y].movementPenalty;
                        if (nodeGrid[addIndex,z,y].state != Enums.NodeState.Air)
                            penaltiesXPass[addIndex,z,y].count++;
                    }
                }
            }
        }

        //Z Pass
        for (int y = 0; y < gridSizeY; y++) {
            for (int x = 0; x < gridSizeX; x++) {
                for (int z = 0; z <= kernelExtents && z < gridSizeZ; z++) {
                    if (nodeGrid[x,z,y].state == Enums.NodeState.Air)
                        continue;
                        
                    penaltiesZPass[x,0,y].value += penaltiesXPass[x,z,y].value;
                    penaltiesZPass[x,0,y].count++;
                }
                for (int z = 1; z < gridSizeZ; z++) {
                    penaltiesZPass[x,z,y].value = penaltiesZPass[x,z-1,y].value;
                    penaltiesZPass[x,z,y].count = penaltiesZPass[x,z-1,y].count;
                    int removeIndex = z - kernelExtents;
                    int addIndex = z + kernelExtents;
                    if (removeIndex >= 0) {
                        penaltiesZPass[x,removeIndex,y].value -= penaltiesXPass[x,removeIndex,y].value;
                        if (nodeGrid[x,removeIndex,y].state != Enums.NodeState.Air)
                            penaltiesZPass[removeIndex,z,y].count--;
                    }
                    if (addIndex < gridSizeZ) {
                        penaltiesZPass[x,addIndex,y].value += penaltiesXPass[x,addIndex,y].value;
                        if (nodeGrid[x,addIndex,y].state != Enums.NodeState.Air)
                            penaltiesZPass[x,addIndex,y].count++;
                    }
                }
            }
        }

        //Y Pass
        for (int x = 0; x < gridSizeX; x++) {
            for (int z = 0; z < gridSizeZ; z++) {
                for (int y = 0; y <= kernelExtents && y < gridSizeY; y++) {
                    if (nodeGrid[x,z,y].state == Enums.NodeState.Air)
                        continue;
                        
                    penaltiesYPass[x,z,0].value += penaltiesZPass[x,z,y].value;
                    penaltiesYPass[x,z,0].count++;
                }

                int blurredPenalty = penaltiesYPass[x,z,0].count > 0 ? 
                                        Mathf.RoundToInt((float)penaltiesYPass[x,z,0].value / penaltiesYPass[x,z,0].count)
                                        : 0;
                nodeGrid[x,z,0].blurredPenalty = blurredPenalty;

                if (blurredPenalty > maxBlurredPenalty)
                    maxBlurredPenalty = blurredPenalty;
                if (blurredPenalty < minBlurredPenalty)
                    minBlurredPenalty = blurredPenalty;

                for (int y = 1; y < gridSizeY; y++) {
                    penaltiesYPass[x,z,y].value = penaltiesYPass[x,z,y-1].value;
                    penaltiesYPass[x,z,y].count = penaltiesYPass[x,z,y-1].count;
                    int removeIndex = y - kernelExtents;
                    int addIndex = y + kernelExtents;
                    if (removeIndex >= 0) {
                        penaltiesYPass[x,z,removeIndex].value -= penaltiesZPass[x,z,removeIndex].value;
                        if (nodeGrid[x,z,removeIndex].state != Enums.NodeState.Air)
                            penaltiesYPass[x,z,removeIndex].count--;
                    }
                    if (addIndex < gridSizeY) {
                        penaltiesYPass[x,z,addIndex].value += penaltiesZPass[x,z,addIndex].value;
                        if (nodeGrid[x,z,addIndex].state != Enums.NodeState.Air)
                            penaltiesYPass[x,z,addIndex].count++;
                    }

                    blurredPenalty = penaltiesYPass[x,z,0].count > 0 ? 
                                        Mathf.RoundToInt((float)penaltiesYPass[x,z,y].value / penaltiesYPass[x,z,y].count)
                                        : 0;
                    nodeGrid[x,z,y].blurredPenalty = blurredPenalty;

                    if (blurredPenalty > maxBlurredPenalty)
                        maxBlurredPenalty = blurredPenalty;
                    if (blurredPenalty < minBlurredPenalty)
                        minBlurredPenalty = blurredPenalty;
                }
            }
        }
    }
    */

    public List<Node> GetNodesWithin(Vector3 min, Vector3 max) {
        List<Node> nodes = new List<Node>();

        int startX = Mathf.Clamp(Mathf.FloorToInt((min.x - worldBottomLeft.x) / nodeSpacing), 0, gridSizeX - 1);
        int startZ = Mathf.Clamp(Mathf.FloorToInt((min.z - worldBottomLeft.z) / nodeSpacing), 0, gridSizeZ - 1);
        int startY = Mathf.Clamp(Mathf.FloorToInt((min.y - worldBottomLeft.y) / nodeSpacing), 0, gridSizeY - 1);
        
        int endX = Mathf.Clamp(Mathf.CeilToInt((max.x - worldBottomLeft.x) / nodeSpacing) - 1, 0, gridSizeX - 1);
        int endZ = Mathf.Clamp(Mathf.CeilToInt((max.z - worldBottomLeft.z) / nodeSpacing) - 1, 0, gridSizeZ - 1);
        int endY = Mathf.Clamp(Mathf.CeilToInt((max.y - worldBottomLeft.y) / nodeSpacing) - 1, 0, gridSizeY - 1);

        for (int x = startX; x <= endX; x++)
        {
            for (int z = startZ; z <= endZ; z++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    nodes.Add(nodeGrid[x,z,y]);
                }
            }
        }

        return nodes;
    }

    public Node GetNodeFromWorldPoint(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(Mathf.Clamp((worldPosition.x - worldBottomLeft.x) / nodeSpacing, 0, gridSizeX - 1));
        int z = Mathf.FloorToInt(Mathf.Clamp((worldPosition.z - worldBottomLeft.z) / nodeSpacing, 0, gridSizeZ - 1));
        int y = Mathf.FloorToInt(Mathf.Clamp((worldPosition.y - worldBottomLeft.y) / nodeSpacing, 0, gridSizeY - 1));

        return nodeGrid[x,z,y];
    }

    public Node GetClosestWalkableNode(Vector3 position, Vector3 size, float stepSize, float maxSlope) {
        Node startNode = GetNodeFromWorldPoint(position);

        Queue<Node> openSet = new Queue<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Enqueue(startNode);
        closedSet.Add(startNode);
        while (openSet.Count > 0) {
            Node targetNode = openSet.Dequeue();
            if (targetNode.CanWalkOn(size, stepSize, maxSlope))
                return targetNode;

            Stack<Node> stack = new Stack<Node>();
            stack.Push(nodeGrid[targetNode.gridX, targetNode.gridZ, Mathf.Clamp(targetNode.gridY + 1, 0, gridSizeY)]); //above
            stack.Push(nodeGrid[targetNode.gridX, targetNode.gridZ, Mathf.Clamp(targetNode.gridY - 1, 0, gridSizeY)]); //below
            stack.Push(nodeGrid[targetNode.gridX, Mathf.Clamp(targetNode.gridZ + 1, 0, gridSizeZ), targetNode.gridY]); //forward
            stack.Push(nodeGrid[targetNode.gridX, Mathf.Clamp(targetNode.gridZ - 1, 0, gridSizeZ), targetNode.gridY]); //backward
            stack.Push(nodeGrid[Mathf.Clamp(targetNode.gridX + 1, 0, gridSizeX), targetNode.gridZ, targetNode.gridY]); //right
            stack.Push(nodeGrid[Mathf.Clamp(targetNode.gridX - 1, 0, gridSizeX), targetNode.gridZ, targetNode.gridY]); //left

            while (stack.Count > 0) {
                Node nextNode = stack.Pop();
                if (!closedSet.Contains(nextNode)) {
                    openSet.Enqueue(nextNode);
                    closedSet.Add(nextNode);
                }
            }
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, gridWorldSize.z));

        if (nodeGrid != null && displayGizmos)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(transform.position + Vector3.up * gridSizeY * nodeSpacing / 2, new Vector3(gridSizeX * nodeSpacing, gridSizeY * nodeSpacing, gridSizeZ * nodeSpacing));
            foreach (Node n in nodeGrid)
            {                    
                switch (n.state) {
                    case Enums.NodeState.Unwalkable:
                        if (displayGizmosUnwalkable) {
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawCube(n.worldPosition, new Vector3(nodeSpacing * 1f,nodeSpacing * 0.1f,nodeSpacing * 1f));
                        }
                        break;
                    case Enums.NodeState.Walkable:
                        if (displayGizmosWalkable) {
                            Gizmos.color = Color.Lerp(Color.green,Color.red, Mathf.InverseLerp(minBlurredPenalty, maxBlurredPenalty, n.blurredPenalty));
                            Gizmos.DrawCube(n.worldPosition, new Vector3(nodeSpacing * 1f,nodeSpacing * 0.1f,nodeSpacing * 1f));
                        }
                        break;
                    case Enums.NodeState.Wall:
                        if (displayGizmosWall) {
                            Gizmos.color = Color.red;
                            Gizmos.DrawSphere(n.worldPosition + Vector3.down * nodeSpacing / 2, nodeSpacing * 0.2f);
                        }
                        break;
                    case Enums.NodeState.Air:
                        if (displayGizmosAir) {
                            Gizmos.color = Color.white;
                            Gizmos.DrawSphere(n.castPoint + Vector3.down * nodeSpacing / 2, nodeSpacing * 0.1f);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    [System.Serializable]
    public class TerrainType
    {
        public LayerMask terrainMask;
        public int terrainPenalty;
    }
}
