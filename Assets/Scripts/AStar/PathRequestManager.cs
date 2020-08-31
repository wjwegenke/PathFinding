using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class PathRequestManager : MonoBehaviour
{
    public static PathRequestManager Instance { get; set; }
    PathFinding pathfinding;
    Queue<PathResult> results = new Queue<PathResult>();

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;

        pathfinding = GetComponent<PathFinding>();
    }

    void Update()
    {
        if (results.Count > 0)
        {
            int itemsInQueue = results.Count;
            lock (results)
            {
                for (int i = 0; i < itemsInQueue; i++)
                {
                    PathResult result = results.Dequeue();
                    result.callback(result.path, result.success);
                }
            }
        }
    }

    public static void RequestPath(PathRequest request)
    {
        ThreadStart threadStart = delegate
        {
            Instance.pathfinding.FindPath(request, Instance.FinishedProcessingPath);
        };
        //Thread newThread = new Thread(threadStart);
        //newThread.Start();
        threadStart.Invoke();
    }

    public void FinishedProcessingPath(PathResult result)
    {
        lock (results)
        {
            results.Enqueue(result);
        }
    }
}

public struct PathRequest
{
    public Vector3 pathStart;
    public Vector3 pathEnd;
    public Action<Vector3[], bool> callback;
    public float radius;
    public bool lineOfSight;
    public float heightOffGround;
    public Transform visualTarget;
    public Vector3 characterSize;
    public float stepSize;
    public float maxSlope;

    public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback)
    {
        pathStart = _start;
        pathEnd = _end;
        callback = _callback;
        radius = 0;
        lineOfSight = false;
        heightOffGround = 0f;
        visualTarget = null;
        characterSize = new Vector3();
        stepSize = 0.5f;
        maxSlope = 0.875f;
    }
    public PathRequest(Vector3 _start, Vector3 _end, float _radius, float _stepSize, Vector3 _characterSize, Action<Vector3[], bool> _callback)
    {
        pathStart = _start;
        pathEnd = _end;
        callback = _callback;
        radius = _radius;
        lineOfSight = false;
        heightOffGround = 0f;
        visualTarget = null;
        characterSize = _characterSize;
        stepSize = _stepSize;
        maxSlope = 0.875f;
    }
    public PathRequest(Vector3 _start, Vector3 _end, float _radius, float _stepSize, Vector3 _characterSize, float _maxSlope, bool _lineOfSight, float _heightOffGround, Transform _visualTarget, Action<Vector3[], bool> _callback)
    {
        pathStart = _start;
        pathEnd = _end;
        callback = _callback;
        radius = _radius;
        lineOfSight = _lineOfSight;
        heightOffGround = _heightOffGround;
        visualTarget = _visualTarget;
        characterSize = _characterSize;
        stepSize = _stepSize;
        maxSlope = _maxSlope;
    }
}

public struct PathResult
{
    public Vector3[] path;
    public bool success;
    public Action<Vector3[], bool> callback;

    public PathResult(Vector3[] path, bool success, Action<Vector3[], bool> callback)
    {
        this.path = path;
        this.success = success;
        this.callback = callback;
    }
}