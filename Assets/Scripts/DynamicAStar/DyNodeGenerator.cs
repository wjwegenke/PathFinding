using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DyNodeGenerator : MonoBehaviour
{
    public event EventHandler PositionHasChanged;
    private bool isDynamic;
    private Collider collider;
    void Awake() {
        isDynamic = transform.tag == "Dynamic";
        collider = transform.GetComponent<Collider>();
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
            DyNodeManager.UpdateClustersWithin(collider.bounds.min, collider.bounds.max);
            OnPositionHasChanged(EventArgs.Empty);
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
}
