using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    private Collider collider;
    private NodeGrid nodeGrid;
    private Vector3[] path;

    private int targetIndex = 0;

    public float stepSize = 0.75f;

    void Awake()
    {
        collider = GetComponent<Collider>();
        GameObject go = GameObject.Find("A*");
        nodeGrid = go.GetComponent<NodeGrid>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 500))
            {
                Vector3 startPosition = transform.position;
                startPosition.y = collider.bounds.min.y;
                PathRequestManager.RequestPath(new PathRequest(startPosition, hit.point, 0.1f, stepSize, collider.bounds.size, OnPathFound));
            }
        }
    }
    

    public void OnPathFound(Vector3[] newPath, bool pathSuccess)
    {
        if (pathSuccess)
        {
            path = newPath;
            targetIndex = 0;
            // currentWaypoint = path[0] + Vector3.up * heightOffGround; //Only used when we were using Update()
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }
    }

    IEnumerator FollowPath() {
        Vector3 currentWaypoint = path[0] + Vector3.up * collider.bounds.size.y / 2;

        while (true) {
            if (transform.position == currentWaypoint)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    yield break;
                }
                currentWaypoint = path[targetIndex] + Vector3.up * collider.bounds.size.y / 2;
            }
            float speed = 5f;
            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed * Time.deltaTime);
            //Update rotation
            // if (lookWhereIWalk && transform.position != currentWaypoint)
            //     lookPosition = currentWaypoint;
            yield return null;
        }
    }

    public void OnDrawGizmos()
    {
        if (path != null)
        {
            for (int i = targetIndex; i < path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(path[i] + Vector3.up * 0.5f, Vector3.one * 0.3f);

                if (i == targetIndex)
                {
                    Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, path[i] + Vector3.up * 0.5f);
                }
                else
                {
                    Gizmos.DrawLine(path[i - 1] + Vector3.up * 0.5f, path[i] + Vector3.up * 0.5f);
                }
            }
        }
    }
}
