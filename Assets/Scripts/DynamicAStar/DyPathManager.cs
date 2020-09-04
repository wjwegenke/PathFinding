using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class DyPathManager : MonoBehaviour
{
    public static DyPathManager Instance { get; set; }
    DyPathFinder dyPathFinder;
    Queue<DyPathResult> results = new Queue<DyPathResult>();

    void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
            return;
        } else {
            Instance = this;
        }
        dyPathFinder = new DyPathFinder();
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
                    DyPathResult result = results.Dequeue();
                    result.callback(result.path, result.success);
                }
            }
        }
    }

    public static void RequestPath(DyPathRequest request)
    {
        ThreadStart threadStart = delegate
        {
            Instance.dyPathFinder.FindPath(request, Instance.FinishedProcessingPath);
        };
        Thread newThread = new Thread(threadStart);
        newThread.Start();
        // threadStart.Invoke();
    }

    public void FinishedProcessingPath(DyPathResult result)
    {
        lock (results)
        {
            results.Enqueue(result);
        }
    }
}

public struct DyPathRequest
{
    public Vector3 pathStart;
    public Vector3 pathEnd;
    public Action<DyNode[], bool> callback;

    public DyPathRequest(Vector3 _start, Vector3 _end, Action<DyNode[], bool> _callback)
    {
        pathStart = _start;
        pathEnd = _end;
        callback = _callback;
    }
}

public struct DyPathResult
{
    public DyNode[] path;
    public bool success;
    public Action<DyNode[], bool> callback;

    public DyPathResult(DyNode[] path, bool success, Action<DyNode[], bool> callback)
    {
        this.path = path;
        this.success = success;
        this.callback = callback;
    }
}
