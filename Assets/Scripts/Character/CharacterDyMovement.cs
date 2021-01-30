using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterDyMovement : MonoBehaviour
{
    public Transform targetTransform;
    public Transform stand;
    public CapsuleCollider movementCollider;
    public MovementCapsule movementCapsule;
    public float maxSlope = 40f;
    public float pathUpdateMoveThreshold = 0.3f;
    public float minPathUpdateTime = 0.2f;
    public float speed = 5f;
    private DyPath currentPath = new DyPath();
    private float heightToCenter = 1f;
    private bool currentPathModified = false;
    private Collider collider;

    public bool drawPath;
    void Awake() {
        collider = GetComponent<Collider>();
        currentPath.NodeModified += OnPathModified;
        movementCapsule = new MovementCapsule(movementCollider.center.y - stand.localPosition.y, movementCollider.height, movementCollider.radius);
        DyNodeManager.Instance.movementCapsules.Add(movementCapsule);
    }
    void Start()
    {
        StartCoroutine(UpdatePath());
        StartCoroutine(FollowPath());
    }

    void Update()
    {
        RaycastHit characterHit;

        if (Physics.Raycast(transform.position, Vector3.down, out characterHit, heightToCenter + 0.1f, DyNodeManager.Instance.movementMask)) {
            transform.parent = characterHit.transform;
        } else {
            transform.parent = null;
        }

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
        if (!currentPath.isAllNodesWalkable || currentPath.isSplitPath || (currentPath.Path.Length > 0 && currentPath.connectedTransforms[0] != this.transform.parent))
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
        Vector3 targetLocalPosOld = targetTransform.localPosition;
        Transform targetParentOld = targetTransform.parent;

		while (true) {
            if (currentPathModified || targetTransform.parent != targetParentOld || (targetTransform.localPosition - targetLocalPosOld).sqrMagnitude > sqrMoveThreshold) {
                currentPathModified = false;
				DyPathManager.RequestPath(new DyPathRequest(stand.position, targetTransform.position, movementCapsule, maxSlope, OnPathFound));
				targetPosOld = targetTransform.position;
                targetLocalPosOld = targetTransform.localPosition;
                targetParentOld = targetTransform.parent;
			}
			yield return null;
		}
	}

    IEnumerator FollowPath() {
        Vector3 targetPosition = transform.position;
		while (true) {
            if (currentPath.Path.Length > 0) {
                targetPosition = currentPath.Path[0].worldPosition + Vector3.up * heightToCenter;
                if ((targetPosition - transform.position).sqrMagnitude < 0.2f) {
                    if (currentPath.Path.Length > 1) {
                        currentPath.SetPath(currentPath.Path.Skip(1).ToArray());
                        targetPosition = currentPath.Path[0].worldPosition + Vector3.up * heightToCenter;
                    } else {
                        currentPath.RemovePath();
                    }
                }
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * speed);
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
            foreach (DyNode n in currentPath.Path) {
                Gizmos.DrawCube(n.worldPosition, Vector3.one * 0.3f);
            }
        }
    }
}
