using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

public class DyNode : IDisposable
{
    public Transform connectedTransform;
    public Vector3 relativePosition;
    public Vector3 worldPosition;
    public float slope = 0f;
    public bool walkable = false;
    public int movementPenalty = 0;
    public float blurredPenalty = 0f;
    public readonly bool isDynamic = false;
    public NodeCluster nodeCluster;
    public Dictionary<DyNode, DyNodeEdge> edges = new Dictionary<DyNode, DyNodeEdge>();
    public HashSet<DyNode> blurNeighbours = new HashSet<DyNode>();
    public HashSet<MovementCapsule> walkableCapsules = new HashSet<MovementCapsule>();
    
    public event EventHandler Modified;

    public DyNode(Transform _connectedTransform, Vector3 _relativePosition) {
        connectedTransform = _connectedTransform;
        relativePosition = _relativePosition;
        worldPosition = connectedTransform.position + relativePosition;

        DyNodeGenerator generator;
        if (connectedTransform.TryGetComponent<DyNodeGenerator>(out generator)) {
            generator.PositionHasChanged += OnPositionHasChanged;
        }

        if (connectedTransform.tag == "Dynamic") {
            isDynamic = true;
        }
    }

    private void ThrowNodeModified() {
        EventHandler handler = Modified;
        if (handler != null)
        {
            handler(this, EventArgs.Empty);
        }
    }

    private void OnPositionHasChanged(object sender, EventArgs e) {
        UpdateSelf();
        UpdateNeighbours();
    }

    public void UpdateSurroundingBlurredPenalties() {
        this.CalculateBlurredPenalty();
        foreach (DyNode blurNeighbour in this.blurNeighbours) {
            blurNeighbour.CalculateBlurredPenalty();
        }
    }

    public void CalculateBlurredPenalty() {
        int count = 1;
        float totalBlur = movementPenalty;
        foreach (DyNode blurNeighbour in this.blurNeighbours) {
            //float sqrMagnitude = (this.worldPosition - blurNeighbours[i].worldPosition).sqrMagnitude;
            totalBlur += blurNeighbour.movementPenalty;
            count++;
        }
        blurredPenalty = totalBlur / count;
    }

    // public void AddNeighbour(DyNode dyNode) {
    //     if (dyNode != this && !this.neighbours.Contains(dyNode)) {
    //         this.neighbours.Add(dyNode);
    //     }
    // }

    // public void RemoveNeighbour(DyNode dyNode) {
    //     if (this.neighbours.Contains(dyNode)) {
    //         this.neighbours.Remove(dyNode);
    //     }
    // }

    public DyNodeEdge GetEdge(DyNode dyNode) {
        if (this.edges.ContainsKey(dyNode))
            return this.edges[dyNode];
        return new DyNodeEdge(this, dyNode);
    }

    public void AddEdge(DyNodeEdge edge) {
        if (edge.targetNode != this) {
            this.edges[edge.targetNode] = edge;
        }
    }

    public void RemoveEdge(DyNodeEdge edge) {
        this.edges.Remove(edge.targetNode);
    }
    public void RemoveEdge(DyNode dyNode) {
        this.edges.Remove(dyNode);
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
        bool startWalkable = walkable;
        Vector3 startWorldPosition = worldPosition;

        worldPosition = connectedTransform.position + relativePosition;
        //Update cluster
        NodeCluster cluster = DyNodeManager.GetClusterFromWorldPosition(this.worldPosition);
        if (cluster != this.nodeCluster) {
            this.nodeCluster?.RemoveDyNode(this);
            cluster.AddDyNode(this);
        }


        if (DyNodeManager.Instance.unwalkableRegionsDictionary.ContainsKey(connectedTransform.gameObject.layer)) {
            walkable = false;
            walkableCapsules.Clear();
        } else {
            //Check if object is on node point
            Collider[] colliders = Physics.OverlapSphere(worldPosition + Vector3.up * 0.05f, 0.05f, DyNodeManager.Instance.movementMask);
            walkable = true;
            if (colliders.Length > 0) {
                foreach (Collider collider in colliders) {
                    if (collider.transform != connectedTransform || !DyNodeManager.Instance.walkableRegionsDictionary.ContainsKey(connectedTransform.gameObject.layer)) {
                        walkable = false;
                        walkableCapsules.Clear();
                        break;
                    }
                }
            }
            if (walkable) { //Check what capsules can walk on this node.
                foreach (MovementCapsule movementCapsule in DyNodeManager.Instance.movementCapsules) {
                    Vector3 point1 = this.worldPosition + Vector3.up * (movementCapsule.heightToCenter - movementCapsule.height / 2f + movementCapsule.radius);
                    Vector3 point2 = this.worldPosition + Vector3.up * (movementCapsule.heightToCenter + movementCapsule.height / 2f - movementCapsule.radius);
                    if (Physics.OverlapCapsule(this.worldPosition + Vector3.up * (movementCapsule.heightToCenter - movementCapsule.height / 2f + movementCapsule.radius),
                                        this.worldPosition + Vector3.up * (movementCapsule.heightToCenter + movementCapsule.height / 2f - movementCapsule.radius),
                                        movementCapsule.radius, DyNodeManager.Instance.movementMask).Length > 0) {
                        walkableCapsules.Remove(movementCapsule);
                    } else {
                        walkableCapsules.Add(movementCapsule);
                    }
                }
            }
            if (walkableCapsules.Count == 0) //No capsule can fit, then set walkable to false
                walkable = false;
        }

        if (startWalkable != walkable || startWorldPosition != worldPosition)
            ThrowNodeModified();
    }

    public void UpdateNeighbours() {
        bool modified = false;
        float maxSqrDist = DyNodeManager.Instance.stepDistance * DyNodeManager.Instance.stepDistance;
        float maxSqrBlurDist = DyNodeManager.Instance.blurDistance * DyNodeManager.Instance.blurDistance;

        int xMin = Mathf.Clamp(nodeCluster.xIndex - 1, 0, DyNodeManager.Instance.xClusters - 1);
        int xMax = Mathf.Clamp(nodeCluster.xIndex + 1, 0, DyNodeManager.Instance.xClusters - 1);
        int yMin = Mathf.Clamp(nodeCluster.yIndex - 1, 0, DyNodeManager.Instance.yClusters - 1);
        int yMax = Mathf.Clamp(nodeCluster.yIndex + 1, 0, DyNodeManager.Instance.yClusters - 1);
        int zMin = Mathf.Clamp(nodeCluster.zIndex - 1, 0, DyNodeManager.Instance.zClusters - 1);
        int zMax = Mathf.Clamp(nodeCluster.zIndex + 1, 0, DyNodeManager.Instance.zClusters - 1);

        //MovementCapsule movementCapsule = new MovementCapsule(1f, 1.4f, 0.1f);

        HashSet<DyNode> neighboursToUpdate = new HashSet<DyNode>();

        //Remove neighbours
        List<DyNodeEdge> removableEdges = new List<DyNodeEdge>();
        List<DyNode> removableBlurNeighbours = new List<DyNode>();
        foreach(DyNodeEdge edge in this.edges.Values) {
            DyNodeEdge edgeFromNode = edge.targetNode.GetEdge(this);
            bool removeNeigbour = false;
            Vector3 direction = (edge.targetNode.worldPosition - this.worldPosition).normalized;
            float distance = Vector3.Distance(edge.targetNode.worldPosition, this.worldPosition);
            if (!this.walkable || !edge.targetNode.walkable || (edge.targetNode.worldPosition - this.worldPosition).sqrMagnitude > maxSqrDist) {
                removeNeigbour = true;
            } else {
                foreach (MovementCapsule movementCapsule in DyNodeManager.Instance.movementCapsules) {
                    if (Physics.CapsuleCast(this.worldPosition + Vector3.up * (movementCapsule.heightToCenter - movementCapsule.height / 2f + movementCapsule.radius),
                                                this.worldPosition + Vector3.up * (movementCapsule.heightToCenter + movementCapsule.height / 2f - movementCapsule.radius),
                                                movementCapsule.radius, direction, distance, DyNodeManager.Instance.movementMask)) {
                        edge.movementCapsules.Remove(movementCapsule);
                        edgeFromNode.movementCapsules.Remove(movementCapsule);
                        //neighboursToUpdate.Add(edge.targetNode);
                    }
                }
            }

            if (removeNeigbour || edge.movementCapsules.Count == 0) {
                removableEdges.Add(edge);
                removableEdges.Add(edgeFromNode);
                modified = true;
            }
        }
        foreach (DyNodeEdge edge in removableEdges) {
            edge.sourceNode.RemoveEdge(edge);
        }
        //Remove blur neighbours
        // int blurNeighbourIdx = 0;
        foreach (DyNode blurNeighbour in blurNeighbours) {
            if ((blurNeighbour.worldPosition - this.worldPosition).sqrMagnitude > maxSqrDist) {
                removableBlurNeighbours.Add(blurNeighbour);
                // this.RemoveBlurNeighbour(blurNeighbour);
                // blurNeighbour.RemoveBlurNeighbour(this);
                //neighboursToUpdate.Add(blurNeighbour);
                // blurNeighbourIdx--;
                modified = true;
            }
            // blurNeighbourIdx++;
        }
        foreach (DyNode blurNeighbour in removableBlurNeighbours) {
            this.RemoveBlurNeighbour(blurNeighbour);
            blurNeighbour.RemoveBlurNeighbour(this);
        }
        if (removableBlurNeighbours.Count > 0) {
            UpdateSurroundingBlurredPenalties();
        }

        //Add neighbours
        if (this.walkable) {
            for (int x = xMin; x <= xMax; x++) {
                for (int y = yMin; y <= yMax; y++) {
                    for (int z = zMin; z <= zMax; z++) {
                        NodeCluster cluster = DyNodeManager.Instance.nodeClusters[x,y,z];
                        cluster.dyNodes.ForEach(dyNode => {
                            float sqrMagnitude = (dyNode.worldPosition - this.worldPosition).sqrMagnitude;
                            if (dyNode != this //Not self
                                && dyNode.walkable //Is Walkable
                                && sqrMagnitude <= maxSqrDist) { //Within distance
                                DyNodeEdge edge = this.GetEdge(dyNode);
                                DyNodeEdge edgeFromNode = dyNode.GetEdge(this);
                                Vector3 direction = (dyNode.worldPosition - this.worldPosition).normalized;
                                float distance = Vector3.Distance(dyNode.worldPosition, this.worldPosition);
                                foreach (MovementCapsule movementCapsule in DyNodeManager.Instance.movementCapsules) {
                                    if (!Physics.CapsuleCast(this.worldPosition + Vector3.up * (movementCapsule.heightToCenter - movementCapsule.height / 2f + movementCapsule.radius),
                                                            this.worldPosition + Vector3.up * (movementCapsule.heightToCenter + movementCapsule.height / 2f - movementCapsule.radius),
                                                            movementCapsule.radius, direction, distance, DyNodeManager.Instance.movementMask)) {
                                        if (!edge.movementCapsules.Contains(movementCapsule)) {
                                            edge.movementCapsules.Add(movementCapsule);
                                            edgeFromNode.movementCapsules.Add(movementCapsule);
                                            //neighboursToUpdate.Add(dyNode);
                                            modified = true;
                                        }
                                    }
                                }
                                if (!this.edges.ContainsKey(edge.targetNode)) {
                                    this.edges.Add(dyNode, edge);
                                    dyNode.edges.Add(this, edgeFromNode);
                                    //neighboursToUpdate.Add(dyNode);
                                    modified = true;
                                }
                            }
                            // else {
                            //     this.RemoveNeighbour(dyNode);
                            //     dyNode.RemoveNeighbour(this);
                            // }

                            //Updated blurredPenalty
                            if (dyNode != this && sqrMagnitude <= maxSqrBlurDist && !this.blurNeighbours.Contains(dyNode)) {
                                this.AddBlurNeighbour(dyNode);
                                dyNode.AddBlurNeighbour(this);
                                //neighboursToUpdate.Add(dyNode);
                                modified = true;
                            }
                        });
                    }
                }
            }
        }

        if (modified) {
            UpdateSurroundingBlurredPenalties();
            ThrowNodeModified();
        }
    }

    // public List<DyNode> GetWalkableNeighbours(Vector3 size) { //Needs work if we're going to use this.
    //     return neighbours.FindAll(neighbour => neighbour.walkable);
    // }

    public bool CanWalkToNode(DyNode dyNode, MovementCapsule movementCapsule) {
        Vector3 point1 = this.worldPosition + Vector3.up * (movementCapsule.heightToCenter - movementCapsule.height / 2f);
        Vector3 point2 = this.worldPosition + Vector3.up * (movementCapsule.heightToCenter + movementCapsule.height / 2f);
        Vector3 direction = (dyNode.worldPosition - this.worldPosition).normalized;
        float distance = Vector3.Distance(dyNode.worldPosition, this.worldPosition);
        if (Physics.CapsuleCast(point1, point2, movementCapsule.radius, direction, distance, DyNodeManager.Instance.movementMask))
            return false;
        return true;
    }

    public void RemoveNeighbours() {
        //Remove neighbours
        foreach (DyNodeEdge edge in this.edges.Values) {
            edge.targetNode.RemoveEdge(this);
        }
        this.edges.Clear();
        // while (neighbours.Count > 0) {
        //     DyNode neighbour = neighbours[neighbours.Count - 1];
        //     this.RemoveNeighbour(neighbour);
        //     neighbour.RemoveNeighbour(this);
        // }
        
        // while (blurNeighbours.Count > 0) {
        //     DyNode blurNeighbour = blurNeighbours[blurNeighbours.Count - 1];
        //     this.RemoveBlurNeighbour(blurNeighbour);
        //     blurNeighbour.RemoveBlurNeighbour(this);
        // }
        foreach (DyNode blurNeighbour in this.blurNeighbours) {
            blurNeighbour.RemoveBlurNeighbour(this);
        }
        this.blurNeighbours.Clear();
    }

    public void DrawGizmos(bool drawNeighbourConnection, bool drawBlurNeighbourConnection) {
        Gizmos.color = !walkable ? Color.red : Color.Lerp(Color.green,Color.magenta, Mathf.InverseLerp(0, 2, blurredPenalty));
        Gizmos.DrawSphere(worldPosition, 0.1f);
        if (drawNeighbourConnection) {
            Gizmos.color = Color.blue;
            // neighbours.ForEach(dyNode => {
            //     Gizmos.DrawLine(this.worldPosition + Vector3.up * 0.01f, dyNode.worldPosition + Vector3.up * 0.01f);
            // });
            foreach (DyNodeEdge edge in this.edges.Values) {
                Gizmos.DrawLine(this.worldPosition + Vector3.up * 0.01f, edge.targetNode.worldPosition + Vector3.up * 0.01f);
            };
        }
        if (drawBlurNeighbourConnection) {
            Gizmos.color = Color.gray;
            foreach (DyNode dyNode in blurNeighbours)
            {
                Gizmos.DrawLine(this.worldPosition + Vector3.up * 0.02f, dyNode.worldPosition + Vector3.up * 0.02f);
            }
        }
    }

    public void Dispose()
    {
        DyNodeGenerator generator;
        if (connectedTransform.TryGetComponent<DyNodeGenerator>(out generator)) {
            generator.PositionHasChanged -= OnPositionHasChanged;
        }
    }
}


public struct DyNodeEdge {
    public DyNode sourceNode;
    public DyNode targetNode;
    public HashSet<MovementCapsule> movementCapsules;

    public DyNodeEdge(DyNode _sourceNode, DyNode _targetNode) {
        this.sourceNode = _sourceNode;
        this.targetNode = _targetNode;
        this.movementCapsules = new HashSet<MovementCapsule>();
    }

    public DyNodeEdge(DyNode _sourceNode, DyNode _targetNode, HashSet<MovementCapsule> _movementCapsules) {
        this.sourceNode = _sourceNode;
        this.targetNode = _targetNode;
        this.movementCapsules = _movementCapsules;
    }
}