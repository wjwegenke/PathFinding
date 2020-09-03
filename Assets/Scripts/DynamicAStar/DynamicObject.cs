using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicObject : MonoBehaviour
{
    public event EventHandler PositionHasChanged;
    void Update()
    {
        if (transform.hasChanged)
        {
            OnPositionHasChanged(EventArgs.Empty);
        }
    }

    private void OnPositionHasChanged(EventArgs e) {
        EventHandler handler = PositionHasChanged;
        if (handler != null)
        {
            handler(this, e);
        }
    }
}
