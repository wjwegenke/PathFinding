﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UnityEngine;

public class DyNode
{
    private Transform connectedTransform;
    public Vector3 relativePosition;
    public Vector3 worldPosition {
        get {
            return connectedTransform.position + relativePosition;
        }
    }
    public float slope = 0f;
    public bool walkable = false;
    public int movementPenalty = 0;
    public float blurredPenalty = 0f;
    public readonly bool isDynamic = false;
    public NodeCluster nodeCluster;
    public List<DyNode> neighbours = new List<DyNode>();
    public ObservableCollection<DyNode> blurNeighbours = new ObservableCollection<DyNode>();

    public DyNode(Transform _connectedTransform, Vector3 _relativePosition) {
        connectedTransform = _connectedTransform;
        relativePosition = _relativePosition;

        DyNodeGenerator generator;
        if (connectedTransform.TryGetComponent<DyNodeGenerator>(out generator)) {
            generator.PositionHasChanged += OnPositionHasChanged;
        }

        if (connectedTransform.tag == "Dynamic") {
            isDynamic = true;
        }

        blurNeighbours.CollectionChanged += OnBlurNeighboursChanged;
    }

    private void OnPositionHasChanged(object sender, EventArgs e) {
        UpdateSelf();
        UpdateNeighbours();
    }
    private void OnBlurNeighboursChanged(object sender, NotifyCollectionChangedEventArgs e) {
        CalculateBlurredPenalty();
    }

    public void CalculateBlurredPenalty() {
        int count = 1;
        float totalBlur = movementPenalty;
        for (int i = 0; i < blurNeighbours.Count; i++) {
            float sqrMagnitude = (this.worldPosition - blurNeighbours[i].worldPosition).sqrMagnitude;
            totalBlur += blurNeighbours[i].movementPenalty;
            count++;
        }
        blurredPenalty = totalBlur / count;
    }

    public void AddNeighbour(DyNode dyNode) {
        if (dyNode != this && !this.neighbours.Contains(dyNode)) {
            this.neighbours.Add(dyNode);
        }
    }

    public void RemoveNeighbour(DyNode dyNode) {
        if (this.neighbours.Contains(dyNode)) {
            this.neighbours.Remove(dyNode);
        }
    }

    public void AddBlurNeighbour(DyNode dyNode) {
        if (dyNode != this && !this.blurNeighbours.Contains(dyNode)) {
            this.blurNeighbours.Add(dyNode);
        }
    }

    public void RemoveBlurNeighbour(DyNode dyNode) {
        if (this.blurNeighbours.Contains(dyNode)) {
            this.blurNeighbours.Remove(dyNode);
        }
    }

    public void UpdateSelf() {
        //Update cluster
        NodeCluster cluster = DyNodeManager.GetClusterFromWorldPosition(this.worldPosition);
        if (cluster != this.nodeCluster) {
            this.nodeCluster.RemoveDyNode(this);
            cluster.AddDyNode(this);
        }

        //Check if object is on node point
        Collider[] colliders = Physics.OverlapSphere(worldPosition, 0, DyNodeManager.Instance.movementMask);
        walkable = true;
        if (colliders.Length > 0) {
            foreach (Collider collider in colliders) {
                if (collider.transform != connectedTransform)
                    walkable = false;
            }
        }
    }

    public void UpdateNeighbours() {
        float maxSqrDist = DyNodeManager.Instance.stepDistance * DyNodeManager.Instance.stepDistance;
        float maxSqrBlurDist = DyNodeManager.Instance.blurDistance * DyNodeManager.Instance.blurDistance;

        int xMin = Mathf.Clamp(nodeCluster.xIndex - 1, 0, DyNodeManager.Instance.xClusters - 1);
        int xMax = Mathf.Clamp(nodeCluster.xIndex + 1, 0, DyNodeManager.Instance.xClusters - 1);
        int yMin = Mathf.Clamp(nodeCluster.yIndex - 1, 0, DyNodeManager.Instance.yClusters - 1);
        int yMax = Mathf.Clamp(nodeCluster.yIndex + 1, 0, DyNodeManager.Instance.yClusters - 1);
        int zMin = Mathf.Clamp(nodeCluster.zIndex - 1, 0, DyNodeManager.Instance.zClusters - 1);
        int zMax = Mathf.Clamp(nodeCluster.zIndex + 1, 0, DyNodeManager.Instance.zClusters - 1);

        //Remove neighbours
        int neighbourIdx = 0;
        while (neighbourIdx < neighbours.Count) {
            DyNode neighbour = neighbours[neighbourIdx];
            if ((neighbour.worldPosition - this.worldPosition).sqrMagnitude > maxSqrDist) {
                this.RemoveNeighbour(neighbour);
                neighbour.RemoveNeighbour(this);
                neighbourIdx--;
            }
            neighbourIdx++;
        }
        int blurNeighbourIdx = 0;
        while (blurNeighbourIdx < blurNeighbours.Count) {
            DyNode blurNeighbour = blurNeighbours[blurNeighbourIdx];
            if ((blurNeighbour.worldPosition - this.worldPosition).sqrMagnitude > maxSqrDist) {
                this.RemoveBlurNeighbour(blurNeighbour);
                blurNeighbour.RemoveBlurNeighbour(this);
                blurNeighbourIdx--;
            }
            blurNeighbourIdx++;
        }

        //Add neighbours
        for (int x = xMin; x <= xMax; x++) {
            for (int y = yMin; y <= yMax; y++) {
                for (int z = zMin; z <= zMax; z++) {
                    NodeCluster cluster = DyNodeManager.Instance.nodeClusters[x,y,z];
                    cluster.dyNodes.ForEach(dyNode => {
                        float sqrMagnitude = (dyNode.worldPosition - this.worldPosition).sqrMagnitude;
                        if (dyNode != this //Not self
                            && sqrMagnitude <= maxSqrDist) { //Within distance
                            this.AddNeighbour(dyNode);
                            dyNode.AddNeighbour(this);
                        }
                        // else {
                        //     this.RemoveNeighbour(dyNode);
                        //     dyNode.RemoveNeighbour(this);
                        // }

                        //Updated blurredPenalty
                        if (dyNode != this && sqrMagnitude <= maxSqrBlurDist) {
                            this.AddBlurNeighbour(dyNode);
                            dyNode.AddBlurNeighbour(this);
                        }
                    });
                }
            }
        }
    }

    public List<DyNode> GetWalkableNeighbours(Vector3 size) {
        return neighbours.FindAll(neighbour => neighbour.walkable);
    }

    public void DrawGizmos() {
        Gizmos.color = !walkable ? Color.red : Color.Lerp(Color.white,Color.black, Mathf.InverseLerp(0, 1, blurredPenalty));
        Gizmos.DrawSphere(worldPosition, 0.1f);
        Gizmos.color = Color.blue;
        neighbours.ForEach(dyNode => {
            Gizmos.DrawLine(this.worldPosition + Vector3.up * 0.01f, dyNode.worldPosition + Vector3.up * 0.01f);
        });
    }
}