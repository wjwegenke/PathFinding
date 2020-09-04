using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DyNodeManager : MonoBehaviour
{
    public static DyNodeManager Instance { get; set; }
    public List<DyNode> dyNodes = new List<DyNode>();
    public float nodeSpacing = 0.5f;
    public float stepDistance = 1f;
    public float maxSlope = 40f;
    public float blurDistance = 3f;
    public Vector3 worldSize;
    public Vector3 bottomLeftPosition;
    public NodeCluster[,,] nodeClusters;
    public int xClusters;
    public int yClusters;
    public int zClusters;
    private float clusterSize;
    [HideInInspector]
    public int dyNodeCount = 0;

    public TerrainType[] walkableRegions;
    public Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    public TerrainType[] unwalkableRegions;
    public Dictionary<int, int> unwalkableRegionsDictionary = new Dictionary<int, int>();

    [HideInInspector]
    public LayerMask walkableMask;
    [HideInInspector]
    public LayerMask unwalkableMask;
    [HideInInspector]
    public LayerMask movementMask;

    public bool drawGizmos = true;
    public bool drawClusters = false;
    public bool drawNodes = true;
    public bool drawNeighbourConnection = false;
    public bool drawBlurNeighbourConnection = false;
    void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
            return;
        } else {
            Instance = this;
        }

        bottomLeftPosition = transform.position;
        
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

        clusterSize = Mathf.Max(blurDistance, stepDistance);
        xClusters = Mathf.CeilToInt(worldSize.x / clusterSize);
        yClusters = Mathf.CeilToInt(worldSize.y / clusterSize);
        zClusters = Mathf.CeilToInt(worldSize.z / clusterSize);
        nodeClusters = new NodeCluster[xClusters, yClusters, zClusters];
        for (int x = 0; x < xClusters; x++) {
            for (int y = 0; y < yClusters; y++) {
                for (int z = 0; z < zClusters; z++) {
                    nodeClusters[x,y,z] = new NodeCluster(x,y,z);
                }
            }
        }
    }

    public static NodeCluster GetClusterFromWorldPosition(Vector3 worldPosition) {
        int x = Mathf.FloorToInt(Mathf.Clamp((worldPosition.x - Instance.bottomLeftPosition.x) / Instance.clusterSize, 0, Instance.xClusters - 1));
        int y = Mathf.FloorToInt(Mathf.Clamp((worldPosition.y - Instance.bottomLeftPosition.y) / Instance.clusterSize, 0, Instance.yClusters - 1));
        int z = Mathf.FloorToInt(Mathf.Clamp((worldPosition.z - Instance.bottomLeftPosition.z) / Instance.clusterSize, 0, Instance.zClusters - 1));

        return Instance.nodeClusters[x,y,z];
    }

    public static DyNode GetDyNodeFromWorldPosition(Vector3 worldPosition) {
        int xBase = Mathf.FloorToInt(Mathf.Clamp((worldPosition.x - Instance.bottomLeftPosition.x) / Instance.clusterSize, 0, Instance.xClusters - 1));
        int yBase = Mathf.FloorToInt(Mathf.Clamp((worldPosition.y - Instance.bottomLeftPosition.y) / Instance.clusterSize, 0, Instance.yClusters - 1));
        int zBase = Mathf.FloorToInt(Mathf.Clamp((worldPosition.z - Instance.bottomLeftPosition.z) / Instance.clusterSize, 0, Instance.zClusters - 1));

        int xMin = Mathf.Clamp(xBase - 1, 0, Instance.xClusters - 1);
        int yMin = Mathf.Clamp(yBase - 1, 0, Instance.yClusters - 1);
        int zMin = Mathf.Clamp(zBase - 1, 0, Instance.zClusters - 1);

        int xMax = Mathf.Clamp(xBase + 1, 0, Instance.xClusters - 1);
        int yMax = Mathf.Clamp(yBase + 1, 0, Instance.yClusters - 1);
        int zMax = Mathf.Clamp(zBase + 1, 0, Instance.zClusters - 1);

        float minSqrDistance = float.MaxValue;
        DyNode dyNode = null;
        for (int x = xMin; x <= xMax; x++) {
            for (int y = yMin; y <= yMax; y++) {
                for (int z = zMin; z <= zMax; z++) {
                    if (Instance.nodeClusters[x,y,z].dyNodes.Count == 0) continue;
                    for (int i = 0; i < Instance.nodeClusters[x,y,z].dyNodes.Count; i++)
                    {
                        float sqrMagnitude = (worldPosition - Instance.nodeClusters[x,y,z].dyNodes[i].worldPosition).sqrMagnitude;
                        if (sqrMagnitude < minSqrDistance) {
                            minSqrDistance = sqrMagnitude;
                            dyNode = Instance.nodeClusters[x,y,z].dyNodes[i];
                        }
                    }
                }
            }
        }

        return dyNode;
    }

    public static void AddDyNode(DyNode dyNode) {
        NodeCluster nodeCluster = GetClusterFromWorldPosition(dyNode.worldPosition);
        nodeCluster.AddDyNode(dyNode);
        dyNode.UpdateNeighbours();
        Instance.dyNodeCount++;
    }

    public static void RemoveDyNode(DyNode dyNode) {
        dyNode.RemoveNeighbours();
        dyNode.nodeCluster.RemoveDyNode(dyNode);
        Instance.dyNodeCount--;
    }

    public static void UpdateClustersWithin(Vector3 min, Vector3 max) {
        int xMin = Mathf.FloorToInt(Mathf.Clamp((min.x - Instance.bottomLeftPosition.x) / Instance.clusterSize, 0, Instance.xClusters - 1));
        int yMin = Mathf.FloorToInt(Mathf.Clamp((min.y - Instance.bottomLeftPosition.y) / Instance.clusterSize, 0, Instance.yClusters - 1));
        int zMin = Mathf.FloorToInt(Mathf.Clamp((min.z - Instance.bottomLeftPosition.z) / Instance.clusterSize, 0, Instance.zClusters - 1));
        
        int xMax = Mathf.FloorToInt(Mathf.Clamp((max.x - Instance.bottomLeftPosition.x) / Instance.clusterSize, 0, Instance.xClusters - 1));
        int yMax = Mathf.FloorToInt(Mathf.Clamp((max.y - Instance.bottomLeftPosition.y) / Instance.clusterSize, 0, Instance.yClusters - 1));
        int zMax = Mathf.FloorToInt(Mathf.Clamp((max.z - Instance.bottomLeftPosition.z) / Instance.clusterSize, 0, Instance.zClusters - 1));

        for (int x = xMin; x <= xMax; x++) {
            for (int y = yMin; y <= yMax; y++) {
                for (int z = zMin; z <= zMax; z++) {
                    Instance.nodeClusters[x,y,z].UpdateNodes();
                }
            }
        }
    }

    private void OnDrawGizmos() {
        if (drawGizmos) {
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(transform.position + Vector3.right * worldSize.x / 2 + Vector3.forward * worldSize.z / 2 + Vector3.up * worldSize.y / 2, worldSize);

            if (drawClusters) {
                Gizmos.color = Color.gray;
                float clusterRadius = clusterSize / 2;
                Vector3 clusterDimensions = Vector3.one * clusterSize;
                if (nodeClusters != null && nodeClusters.Length > 0) {
                    foreach (NodeCluster cluster in nodeClusters)
                    {
                        Vector3 center = Instance.transform.position
                                        + Vector3.right * (cluster.xIndex * clusterSize + clusterRadius)
                                        + Vector3.up * (cluster.yIndex * clusterSize + clusterRadius)
                                        + Vector3.forward * (cluster.zIndex * clusterSize + clusterRadius);
                        Gizmos.DrawWireCube(center, clusterDimensions);                
                    }
                }
            }

            if (drawNodes) {
                for (int x = 0; x < xClusters; x++) {
                    for (int y = 0; y < yClusters; y++) {
                        for (int z = 0; z < zClusters; z++) {
                            NodeCluster cluster = nodeClusters[x,y,z];
                            cluster.dyNodes.ForEach(dyNode => {
                                dyNode.DrawGizmos(drawNeighbourConnection, drawBlurNeighbourConnection);
                            });
                        }
                    }
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
