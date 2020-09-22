using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MovementCapsule
{
    public float heightToCenter;
    public float height;
    public float radius;
    
    public MovementCapsule(float _heightToCenter, float _height, float _radius) {
        this.heightToCenter = _heightToCenter;
        this.height = _height;
        this.radius = _radius;
    }
}
