using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterDyMovement : MonoBehaviour
{
    public Transform targetTransform;
    public float pathUpdateMoveThreshold = 0.5f;
    public float minPathUpdateTime = 0.2f;
    private DyPath currentPath = new DyPath();
    private bool currentPathModified = false;
    private Collider collider;

    public bool drawPath;
    void Awake() {
        collider = GetComponent<Collider>();
        currentPath.NodePositionHasChanged += OnPathModified;
    }
    void Start()
    {
        StartCoroutine(UpdatePath());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 500, DyNodeManager.Instance.movementMask))
            {
                targetTransform.position = hit.point;
                targetTransform.parent = hit.transform;
            }
        }
    }

    private void OnPathModified(object sender, EventArgs e) {
        currentPathModified = true;
    }

    public void OnPathFound(DyNode[] path, bool pathSuccess)
    {
        if (pathSuccess)
        {
            currentPath.SetPath(path);
        } else {
            currentPath.RemovePath();
        }
    }

    IEnumerator UpdatePath() {

		if (Time.timeSinceLevelLoad < .3f) {
			yield return new WaitForSeconds (.3f);
		}

		float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
		Vector3 targetPosOld = targetTransform.position;

		while (true) {
            if (currentPathModified || (targetTransform.position - targetPosOld).sqrMagnitude > sqrMoveThreshold) {
                currentPathModified = false;
                Vector3 startPosition = transform.position;
                startPosition.y = collider.bounds.min.y;
				DyPathManager.RequestPath(new DyPathRequest(startPosition, targetTransform.position, OnPathFound));
				targetPosOld = targetTransform.position;
			}
			yield return null;
		}
	}

    private void OnDestroy() {
        currentPath.Dispose();
    }

    private void OnDrawGizmos() {
        if (drawPath && currentPath != null) {
            Gizmos.color = Color.black;
            foreach (DyNode n in currentPath.path) {
                Gizmos.DrawCube(n.worldPosition, Vector3.one * 0.3f);
            }
        }
    }
}
