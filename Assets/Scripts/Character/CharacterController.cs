using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public bool displayGizmos = false;
    private Collider collider;
    private NodeGrid nodeGrid;
    private Path path;
    private int pathIndex;
    public bool goToExactLocation = false;
    float minLookDist = 0.75f;

    private int targetIndex = 0;


    
	const float minPathUpdateTime = .2f;
	const float minCurrentPathUpdateTime = .5f;
	const float pathUpdateMoveThreshold = .5f;
    
    private bool forceRequestNewPath = false;

	public Transform targetTransform;
    private Vector3 targetPosition;
	public float speed = 8;
    public float verticalSpeed = 15;
	public float turnSpeed = 8;
	public float turnDst = 1;
	public float stoppingDst = 1;
	public float stopDst = 0.1f;
    public float stepSize = 0.7f;
    public float maxSlope = 0.875f;

    private bool isStuck = false;

    private float heightToCenter {
        get { return collider.bounds.size.y / 2f; }
    }

    private Vector3 feetPosiiton {
        get { return transform.position + Vector3.down * heightToCenter; }
    }

    private void Awake()
    {
        collider = GetComponent<Collider>();
        GameObject go = GameObject.Find("A*");
        nodeGrid = go.GetComponent<NodeGrid>();
    }

    private void Start() {
        targetTransform.position = transform.position;
        targetTransform.parent = transform;
        StartCoroutine(UpdatePath());
        StartCoroutine(UpdateCurrentPath());
        StartCoroutine(StayUnstuck());
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 500, nodeGrid.movementMask))
            {
                //targetTransform.position = nodeGrid.GetNodeFromWorldPoint(hit.point).worldPosition;
                targetTransform.position = hit.point;
                targetTransform.parent = hit.transform;
                // StopCoroutine("UpdatePath");
                // StartCoroutine("UpdatePath");
                //PathRequestManager.RequestPath(new PathRequest(startPosition, hit.point, 0.1f, stepSize, collider.bounds.size, OnPathFound));
            }
        }

        if (!isStuck) {
            //Move up/down
            float radius = Mathf.Min(collider.bounds.size.x, collider.bounds.size.z) * 0.25f;
            RaycastHit groundHit;
            if (Physics.SphereCast(transform.position, radius, Vector3.down, out groundHit, stepSize + heightToCenter, nodeGrid.movementMask)) {
                transform.position = new Vector3(transform.position.x, Vector3.MoveTowards(transform.position, groundHit.point + Vector3.up * heightToCenter, verticalSpeed * Time.deltaTime).y, transform.position.z);

                //Change parent for elevators
                if (groundHit.transform.tag == "Elevator") {
                    // if (transform.parent != groundHit.transform) {
                    //     targetTransform.position = new Vector3(transform.position.x, collider.bounds.min.y, transform.position.z);
                    //     // targetPosition = transform.position;
                    //     //targetPosition.y = collider.bounds.min.y;
                    // }
                    transform.parent = groundHit.transform;
                } else if (transform.parent != null) {
                    transform.parent = null;
                    //targetTransform.position = new Vector3(transform.position.x, collider.bounds.min.y, transform.position.z);
                    // targetPosition = transform.position;
                    // targetPosition.y = collider.bounds.min.y;
                }
            } else { //Nothing underneath character
                transform.position = new Vector3(transform.position.x, Vector3.MoveTowards(transform.position, transform.position + Vector3.down * 2f, verticalSpeed * Time.deltaTime).y, transform.position.z);
            }

            //Change target parent for elevators
            float sqrStopDst = stopDst * stopDst;
            Vector3 targetPosition = (goToExactLocation ? targetTransform.position : nodeGrid.GetNodeFromWorldPoint(targetTransform.position).worldPosition) + Vector3.up * heightToCenter;
            if ((targetPosition - transform.position).sqrMagnitude <= sqrStopDst) {
                targetTransform.position = transform.position;
                targetTransform.parent = transform;
            } else if (targetTransform.parent != transform && Physics.Raycast(targetTransform.position + Vector3.up * heightToCenter, Vector3.down, out groundHit, stepSize + heightToCenter, nodeGrid.movementMask)) {
                targetTransform.parent = groundHit.transform;
            } 
            // else {
            //     targetTransform.position = transform.position;
            //     targetTransform.parent = transform;
            // }

        }
    }

    private bool CheckStuck() {
        Node node = nodeGrid.GetNodeFromWorldPoint(transform.position + Vector3.down * heightToCenter);
        return !node.CanWalkOn(collider.bounds.size, stepSize, maxSlope);
    }

    private bool CheckLOSNextLookPoint(int startIndex) {
        if (path == null || path.lookPoints.Length <= startIndex) return true;

        //Can character see next point?
        Vector3 start = transform.position;
        Vector3 target = path.lookPoints[startIndex] + Vector3.up * heightToCenter;
        Vector3 direction = target - start;
        RaycastHit hit;
        if (Physics.Raycast(start, direction, out hit, Vector3.Distance(start, target), nodeGrid.movementMask)) {
            Debug.Log("No LOS");
            return false;
        }

        return true;
    }

    public void OnPathFound(Vector3[] waypoints, bool pathSuccess)
    {
        if (pathSuccess)
        {
            path = new Path(waypoints, transform.position, turnDst, stoppingDst);
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        } else {
            Debug.Log("Bad path");
            targetTransform.position = transform.position;
            targetTransform.parent = transform;
        }
    }

    public void OnUpdatedPathFound(Vector3[] waypoints, bool pathSuccess)
    {
        if (pathSuccess && path != null)
        {
            waypoints = waypoints.RemoveAt(0);
            if (!path.CheckSamePathFromEnd(waypoints)) {
                path = new Path(waypoints, transform.position, turnDst, stoppingDst);
                StopCoroutine("FollowPath");
                StartCoroutine("FollowPath");
            }
        } else {
            Debug.Log("Bad updated path");
            StopCoroutine("FollowPath");
            path = null;
            targetTransform.position = transform.position;
            targetTransform.parent = transform;
        }
    }

    IEnumerator StayUnstuck() {
        while (true) {
            yield return new WaitForSeconds(0.1f);
            Vector3 stuckPosition;
            if (CheckStuck()) {
                stuckPosition = transform.position;
                yield return new WaitForSeconds(0.1f);
                if (stuckPosition == transform.position && CheckStuck()) { //Check if we're still stuck after a moment.
                    isStuck = true;
                    Debug.Log("Stuck");
                    StopCoroutine("FollowPath");
                    path = null;
                    targetTransform.position = transform.position;
                    targetTransform.parent = transform;

                    Node goToNode = nodeGrid.GetClosestWalkableNode(feetPosiiton, collider.bounds.size, stepSize, maxSlope);
                    Vector3 goToPosition = goToNode.worldPosition + Vector3.up * heightToCenter;
                    float sqrMinMagnitude = 0.05f;
                    while ((goToPosition - transform.position).sqrMagnitude > sqrMinMagnitude) {
                        transform.position = Vector3.MoveTowards(transform.position, goToPosition, verticalSpeed * Time.deltaTime);
                        yield return null;
                    }
                    isStuck = false;
                }
            } else {
                isStuck = false;
            }
        }
    }

    IEnumerator UpdatePath() {

		if (Time.timeSinceLevelLoad < .3f) {
			yield return new WaitForSeconds (.3f);
		}
        // Vector3 startPosition = transform.position;
        // startPosition.y = collider.bounds.min.y;
        // PathRequestManager.RequestPath(new PathRequest(startPosition, targetPosition, 0.1f, stepSize, collider.bounds.size, OnPathFound));

		float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
		Vector3 targetPosOld = targetTransform.position;

		while (true) {
            if (!isStuck && targetTransform.parent != transform && (targetTransform.position - targetPosOld).sqrMagnitude > sqrMoveThreshold) {
                //forceRequestNewPath = false;
                Vector3 startPosition = transform.position;
                startPosition.y = collider.bounds.min.y;
				PathRequestManager.RequestPath(new PathRequest(startPosition, targetTransform.position, 0.1f, stepSize, collider.bounds.size, OnPathFound));
				targetPosOld = targetTransform.position;
			}
			yield return new WaitForSeconds(minPathUpdateTime);
		}
	}

    IEnumerator UpdateCurrentPath() {
        while (true) {
            if (path != null && path.lookPoints.Length > 0 && pathIndex < path.lookPoints.Length) {
                Vector3 startPosition = transform.position;
                startPosition.y = collider.bounds.min.y;
                int startIndex = pathIndex - 1;
                if (startIndex >= 0) {
                    startPosition = path.lookPoints[startIndex];
                }
                PathRequestManager.RequestPath(new PathRequest(startPosition, targetTransform.position, 0.1f, stepSize, collider.bounds.size, OnUpdatedPathFound));
            }
            yield return new WaitForSeconds(minCurrentPathUpdateTime);
        }
    }

    IEnumerator FollowPath() {
        bool followingPath = true;
		pathIndex = 0;
        targetPosition = path.lookPoints[0] + Vector3.up * heightToCenter;
        // if (sqrDstToTarget >= minLookDist * minLookDist)
		//     transform.LookAt(targetPosition);

		float speedPercent = 1;

		while (followingPath) {
            //Get goto point
			Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);
			while (path.turnBoundaries[pathIndex].HasCrossedLine(pos2D)) {
				if (pathIndex == path.finishLineIndex) {
					followingPath = false;
					break;
				} else {
					pathIndex++;
				}
			}

			if (followingPath) {
                Vector3 goToPosition = path.lookPoints[pathIndex];
                if (pathIndex == path.finishLineIndex) {
                    if (goToExactLocation) {
                        if (targetTransform.parent == transform) {
                            followingPath = false;
                            break;
                        }
                        goToPosition = targetTransform.position;
                    }

                    //Check if we're close enough
                    float sqrDstToEnd = (transform.position - (goToPosition + Vector3.up * heightToCenter)).sqrMagnitude;
                    float sqrStopDst = stopDst * stopDst;
                    float sqrStoppingDst = stoppingDst * stoppingDst;
                    if (sqrDstToEnd <= sqrStopDst) {
                        followingPath = false;
                        break;
                    } else if (sqrDstToEnd <= stoppingDst * stoppingDst) {
                        speedPercent = Mathf.Clamp01(sqrDstToEnd / stoppingDst);
                        if (speedPercent < 0.01f) {
                            followingPath = false;
                        }
                    }
                }

                //Calculate speed percent
				// if (pathIndex >= path.slowDownIndex && stoppingDst > 0) {
				// 	speedPercent = Mathf.Clamp01(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(pos2D) / stoppingDst);
				// 	if (speedPercent < 0.01f) {
				// 		followingPath = false;
				// 	} else if (speedPercent < 0.3f) {
                //         speedPercent = 0.3f; //Setting minimum.
                //     }
				// }

                //Rotate
                // Vector3 lookDirection = path.lookPoints[pathIndex] - transform.position;
                // lookDirection.y = 0;
				// Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
				// transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed); //Points it down a bit.
                // transform.rotation = Quaternion.Euler(new Vector3(0f, transform.rotation.eulerAngles.y, 0f));

                //Move
				//transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self); //Move forward
                //transform.position = new Vector3(transform.position.x, Vector3.MoveTowards(transform.position, path.lookPoints[pathIndex] + Vector3.up * heightToCenter, speedByGravity * Time.deltaTime).y, transform.position.z); //Move up/down

                //targetPosition = Vector3.Lerp(targetPosition, path.lookPoints[pathIndex] + Vector3.up * heightToCenter, Time.deltaTime * turnSpeed);
                targetPosition = Vector3.MoveTowards(targetPosition, goToPosition + Vector3.up * heightToCenter, Time.deltaTime * speed);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * speed * speedPercent);
                //transform.Translate((targetPosition - transform.position).normalized * Time.deltaTime * speed * speedPercent, Space.World); //Move forward

                Vector2 targetPos2D = new Vector2(targetPosition.x, targetPosition.y);
                float sqrDstToTarget2D = (pos2D - targetPos2D).sqrMagnitude;
                if (sqrDstToTarget2D >= minLookDist) {
                    Vector3 lookDirection = targetPosition - transform.position;
                    lookDirection.y = 0;
                    if (lookDirection != Vector3.zero) {
                        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed); //Points it down a bit.
                        transform.rotation = Quaternion.Euler(new Vector3(0f, transform.rotation.eulerAngles.y, 0f));
                    }
                }
			}

			yield return null;
		}
        path = null;
    }

    public void OnDrawGizmos()
    {
        if (displayGizmos && path != null) {
			path.DrawWithGizmos();

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(targetPosition, 1f);
		}
    }
}
