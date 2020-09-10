using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DyPath : IDisposable
{
    private DyNode[] _Path;
    public DyNode[] Path { 
        get {
            return _Path;
        }
        private set {
            _Path = value;
            isAllNodesWalkable = GetIsAllNodesWalkable();
            isSplitPath = GetIsSplitPath();
            connectedTransforms = GetConnectedTransforms();
        }
    }

    public bool isSplitPath;
    public bool isAllNodesWalkable;
    public Transform[] connectedTransforms;
    public event EventHandler NodeModified;

    public DyPath() {
        this.Path = new DyNode[0];
    }

    public DyPath(DyNode[] _path) {
        this.Path = _path;
        foreach (DyNode dyNode in Path) {
            dyNode.Modified += OnNodeModified;
        }
    }

    private void OnNodeModified(object sender, EventArgs e) {
        EventHandler handler = NodeModified;
        if (handler != null)
        {
            handler(this, EventArgs.Empty);
        }
    }

    private bool GetIsAllNodesWalkable() {
        if (Path == null || Path.Length == 0) return false;
        for (int i = 0; i < Path.Length; i++) {
            if (!Path[i].walkable)
                return false;
        }
        return true;
    }

    private bool GetIsSplitPath() {
        if (Path == null || Path.Length <= 1) return false;
        for (int i = 1; i < Path.Length; i++) {
            if (Path[i].connectedTransform != Path[i-1].connectedTransform)
                return true;
        }
        return false;
    }

    private Transform[] GetConnectedTransforms() {
        if (Path == null || Path.Length == 0) return new Transform[0];
        HashSet<Transform> connectedTransformsHash = new HashSet<Transform>();
        for (int i = 0; i < Path.Length; i++) {
            if (!connectedTransformsHash.Contains(Path[i].connectedTransform))
                connectedTransformsHash.Add(Path[i].connectedTransform);
        }
        Transform[] connectedTransforms = new Transform[connectedTransformsHash.Count];
        connectedTransformsHash.CopyTo(connectedTransforms);
        return connectedTransforms;
    }

    public void SetPath(DyNode[] _path) {
        foreach (DyNode dyNode in Path) {
            dyNode.Modified -= OnNodeModified;
        }
        this.Path = _path;
        foreach (DyNode dyNode in Path) {
            dyNode.Modified += OnNodeModified;
        }
    }

    internal void RemovePath()
    {
        foreach (DyNode dyNode in Path) {
            dyNode.Modified -= OnNodeModified;
        }
        Path = new DyNode[0];
    }

    public void Dispose()
    {
        if (Path != null) {
            foreach (DyNode dyNode in Path) {
                dyNode.Modified -= OnNodeModified;
            }
            Path = null;
        }
    }
}
