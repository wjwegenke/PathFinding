using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DyPath : IDisposable
{
    public DyNode[] path { get; private set; }
    public event EventHandler NodePositionHasChanged;

    public DyPath() {
        this.path = new DyNode[0];
    }

    public DyPath(DyNode[] _path) {
        this.path = _path;
        foreach (DyNode dyNode in path) {
            dyNode.PositionHasChanged += OnNodePositionHasChanged;
        }
    }

    private void OnNodePositionHasChanged(object sender, EventArgs e) {
        EventHandler handler = NodePositionHasChanged;
        if (handler != null)
        {
            handler(this, EventArgs.Empty);
        }
    }

    public void SetPath(DyNode[] _path) {
        foreach (DyNode dyNode in path) {
            dyNode.PositionHasChanged -= OnNodePositionHasChanged;
        }
        this.path = _path;
        foreach (DyNode dyNode in path) {
            dyNode.PositionHasChanged += OnNodePositionHasChanged;
        }
    }

    internal void RemovePath()
    {
        foreach (DyNode dyNode in path) {
            dyNode.PositionHasChanged -= OnNodePositionHasChanged;
        }
        path = new DyNode[0];
    }

    public void Dispose()
    {
        if (path != null) {
            foreach (DyNode dyNode in path) {
                dyNode.PositionHasChanged -= OnNodePositionHasChanged;
            }
            path = null;
        }
    }
}
