using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DyNodeCreator : MonoBehaviour
{
    public Vector3 size;
    public float stepSize;

    

    void Start()
    {
        float xRadius = size.x / 2;
        float zRadius = size.z / 2;
        int xSteps = Mathf.FloorToInt(xRadius / stepSize);
        int zSteps = Mathf.FloorToInt(zRadius / stepSize);

        for (int x = -xSteps; x <= xSteps; x++) {
            for (int z = -zSteps; z <= zSteps; z++) {
                Vector3 castPoint = transform.position + new Vector3(x * stepSize, size.y, z * stepSize);

                RaycastHit[] hits = Physics.RaycastAll(castPoint, Vector3.down, size.y + 0.1f, DyNodeManager.Instance.movementMask);
                foreach (RaycastHit hit in hits) {
                    //Check if a node can actually exist here.
                    bool createNode = true;
                    if (hit.transform.tag != "Dynamic") {
                        Collider[] colliders = Physics.OverlapSphere(hit.point, 0, DyNodeManager.Instance.movementMask);
                        if (colliders.Length > 0) {
                            foreach (Collider collider in colliders) {
                                if (collider.transform != hit.transform && collider.tag != "Dynamic")
                                    createNode = false;
                            }
                        }
                    }

                    if (createNode) {
                        DyNode dyNode = new DyNode(hit.transform, hit.point - hit.transform.position);
                        DyNodeManager.AddDyNode(dyNode);
                    }
                }
            }
        }
    }
}
