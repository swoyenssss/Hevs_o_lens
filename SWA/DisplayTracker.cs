using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HEVS;
using OSCsharp.Data;
using OSCsharp.Net;
using OSCsharp.Utils;
using System;
using UnityEngine.XR;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Input;

/// <summary>
/// </summary>
public class DisplayTracker : ClusterTransform
{

    /// <summary>
    /// The ID for the display's config.
    /// </summary>
    public string displayID;

    /// <summary>
    /// The actual display config.
    /// </summary>
    public DisplayConfig display;

    void FixedUpdate()
    {
        // Update transform if there is a receiver
        if (Cluster.isMaster)
        {
            transform.position = display.transform.translate;
            transform.rotation = display.transform.rotate;
        }
    }
}