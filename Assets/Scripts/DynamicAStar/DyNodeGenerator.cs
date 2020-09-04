using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DyNodeGenerator : MonoBehaviour
{
    public event EventHandler PositionHasChanged;
    private bool isDynamic;
    private Collider collider;
    private Bounds oldBounds;
    private List<DyNode> dyNodes = new List<DyNode>();
    void Awake() {
        isDynamic = transform.tag == "Dynamic";
        collider = transform.GetComponent<Collider>();
        oldBounds = collider.bounds;
    }

    void Start()
    {
        float spacing = DyNodeManager.Instance.nodeSpacing;
        float xRadius = collider.bounds.extents.x;
        float zRadius = collider.bounds.extents.z;
        float yRadius = collider.bounds.extents.y;
        float height = collider.bounds.size.y;
        int xSteps = Mathf.CeilToInt(xRadius / spacing);
        int zSteps = Mathf.CeilToInt(zRadius / spacing);

        for (int x = -xSteps; x <= xSteps; x++) {
            for (int z = -zSteps; z <= zSteps; z++) {
                Vector3 castPoint = collider.bounds.center + new Vector3(x * spacing, yRadius, z * spacing) + Vector3.up * (yRadius + 0.1f);
                castPoint.x = Mathf.Clamp(castPoint.x, collider.bounds.min.x, collider.bounds.max.x);
                castPoint.z = Mathf.Clamp(castPoint.z, collider.bounds.min.z, collider.bounds.max.z);

                RaycastHit[] hits = Physics.RaycastAll(castPoint, Vector3.down, height + 0.2f);
                foreach (RaycastHit hit in hits) {
                    if (hit.transform != this.transform) continue;
                    //Check if a node can actually exist here.
                    bool createNode = true;
                    //Check if object is on node point
                    if (!isDynamic) {
                        Collider[] colliders = Physics.OverlapSphere(hit.point, 0, DyNodeManager.Instance.movementMask);
                        if (colliders.Length > 0) {
                            foreach (Collider collider in colliders) {
                                if (collider.transform != this.transform && collider.tag != "Dynamic")
                                    createNode = false;
                            }
                        }
                    }

                    if (createNode) {
                        DyNode dyNode = new DyNode(hit.transform, hit.point - hit.transform.position);
                        dyNode.slope = Mathf.Acos(hit.normal.y) * 180f / Mathf.PI;
                        if (!DyNodeManager.Instance.walkableRegionsDictionary.TryGetValue(hit.transform.gameObject.layer, out dyNode.movementPenalty))
                            DyNodeManager.Instance.unwalkableRegionsDictionary.TryGetValue(hit.transform.gameObject.layer, out dyNode.movementPenalty);
                        dyNode.blurredPenalty = dyNode.movementPenalty;
                        dyNodes.Add(dyNode);
                        DyNodeManager.AddDyNode(dyNode);
                    }
                }
            }
        }
    }
    void Update()
    {
        if (transform.hasChanged)
        {
            if (collider.bounds.min.x <= oldBounds.max.x && collider.bounds.min.y <= oldBounds.max.y && collider.bounds.min.z <= oldBounds.max.z
                && collider.bounds.max.x >= oldBounds.min.x && collider.bounds.max.y >= oldBounds.min.y && collider.bounds.max.z >= oldBounds.min.z) {
                Vector3 min = new Vector3(Mathf.Min(collider.bounds.min.x, oldBounds.min.x), Mathf.Min(collider.bounds.min.y, oldBounds.min.y), Mathf.Min(collider.bounds.min.z, oldBounds.min.z));
                Vector3 max = new Vector3(Mathf.Max(collider.bounds.max.x, oldBounds.max.x), Mathf.Max(collider.bounds.max.y, oldBounds.max.y), Mathf.Max(collider.bounds.max.z, oldBounds.max.z));
                DyNodeManager.UpdateClustersWithin(min, max);
            } else {
                DyNodeManager.UpdateClustersWithin(oldBounds.min, oldBounds.max);
                DyNodeManager.UpdateClustersWithin(collider.bounds.min, collider.bounds.max);
            }
            OnPositionHasChanged(EventArgs.Empty);
            oldBounds = collider.bounds;
            transform.hasChanged = false;
        }
    }

    private void OnPositionHasChanged(EventArgs e) {
        EventHandler handler = PositionHasChanged;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    private void OnDestroy() {
        foreach (DyNode dyNode in dyNodes) {
            DyNodeManager.RemoveDyNode(dyNode);
            dyNode.Dispose();
        }
    }
}
