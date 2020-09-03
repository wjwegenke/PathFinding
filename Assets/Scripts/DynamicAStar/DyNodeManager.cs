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
    public NodeCluster[,,] nodeClusters;
    public int xClusters;
    public int yClusters;
    public int zClusters;

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
    void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
            return;
        } else {
            Instance = this;
        }

        
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

        xClusters = Mathf.CeilToInt(worldSize.x / blurDistance);
        yClusters = Mathf.CeilToInt(worldSize.y / blurDistance);
        zClusters = Mathf.CeilToInt(worldSize.z / blurDistance);
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
        int x = Mathf.FloorToInt(Mathf.Clamp((worldPosition.x - Instance.transform.position.x) / Instance.blurDistance, 0, Instance.xClusters - 1));
        int y = Mathf.FloorToInt(Mathf.Clamp((worldPosition.y - Instance.transform.position.y) / Instance.blurDistance, 0, Instance.yClusters - 1));
        int z = Mathf.FloorToInt(Mathf.Clamp((worldPosition.z - Instance.transform.position.z) / Instance.blurDistance, 0, Instance.zClusters - 1));

        return Instance.nodeClusters[x,y,z];
    }

    public static void AddDyNode(DyNode dyNode) {
        NodeCluster nodeCluster = GetClusterFromWorldPosition(dyNode.worldPosition);
        nodeCluster.AddDyNode(dyNode);
        dyNode.UpdateNeighbours();
    }

    public static void UpdateClustersWithin(Vector3 min, Vector3 max) {
        int xMin = Mathf.FloorToInt(Mathf.Clamp((min.x - Instance.transform.position.x) / Instance.blurDistance, 0, Instance.xClusters - 1));
        int yMin = Mathf.FloorToInt(Mathf.Clamp((min.y - Instance.transform.position.y) / Instance.blurDistance, 0, Instance.yClusters - 1));
        int zMin = Mathf.FloorToInt(Mathf.Clamp((min.z - Instance.transform.position.z) / Instance.blurDistance, 0, Instance.zClusters - 1));
        
        int xMax = Mathf.FloorToInt(Mathf.Clamp((max.x - Instance.transform.position.x) / Instance.blurDistance, 0, Instance.xClusters - 1));
        int yMax = Mathf.FloorToInt(Mathf.Clamp((max.y - Instance.transform.position.y) / Instance.blurDistance, 0, Instance.yClusters - 1));
        int zMax = Mathf.FloorToInt(Mathf.Clamp((max.z - Instance.transform.position.z) / Instance.blurDistance, 0, Instance.zClusters - 1));

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

            Gizmos.color = Color.gray;
            float clusterRadius = blurDistance / 2;
            Vector3 clusterSize = Vector3.one * blurDistance;
            if (nodeClusters != null && nodeClusters.Length > 0) {
                foreach (NodeCluster cluster in nodeClusters)
                {
                    Vector3 center = Instance.transform.position
                                    + Vector3.right * (cluster.xIndex * blurDistance + clusterRadius)
                                    + Vector3.up * (cluster.yIndex * blurDistance + clusterRadius)
                                    + Vector3.forward * (cluster.zIndex * blurDistance + clusterRadius);
                    Gizmos.DrawWireCube(center, clusterSize);                
                }
            }

            for (int x = 0; x < xClusters; x++) {
                for (int y = 0; y < yClusters; y++) {
                    for (int z = 0; z < zClusters; z++) {
                        NodeCluster cluster = nodeClusters[x,y,z];
                        cluster.dyNodes.ForEach(dyNode => {
                            dyNode.DrawGizmos();
                        });
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
