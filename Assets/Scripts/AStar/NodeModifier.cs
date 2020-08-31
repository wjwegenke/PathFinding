using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeModifier : MonoBehaviour
{
    private Collider collider;
    private Vector3 prevMinBound;
    private Vector3 prevMaxBound;
    private NodeGrid nodeGrid;
    void Awake()
    {
        GameObject go = GameObject.Find("A*");
        nodeGrid = go.GetComponent<NodeGrid>();
        collider = GetComponent<Collider>();
        prevMinBound = collider.bounds.min;
        prevMaxBound = collider.bounds.max;
    }

    void Update()
    {
        if (transform.hasChanged)
        {
            Vector3 minBound = new Vector3(Mathf.Min(prevMinBound.x, collider.bounds.min.x), Mathf.Min(prevMinBound.y, collider.bounds.min.y), Mathf.Min(prevMinBound.z, collider.bounds.min.z));
            Vector3 maxBound = new Vector3(Mathf.Max(prevMaxBound.x, collider.bounds.max.x), Mathf.Max(prevMaxBound.y, collider.bounds.max.y), Mathf.Max(prevMaxBound.z, collider.bounds.max.z));

            nodeGrid.RecalculateNodes(minBound, maxBound);

            transform.hasChanged = false;
            prevMinBound = collider.bounds.min;
            prevMaxBound = collider.bounds.max;
        }
    }
}
