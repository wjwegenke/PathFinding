using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    private bool directionForward = true;
    public Transform startPosition;
    public Transform endPosition;
    public float speed = 3;

    void Update()
    {
        Vector3 targetPosition = startPosition.position;
        if (directionForward) 
            targetPosition = endPosition.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (transform.position == targetPosition)
            directionForward = !directionForward;
    }
}
