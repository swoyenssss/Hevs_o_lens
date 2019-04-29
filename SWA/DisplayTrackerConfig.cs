using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HEVS;

/// <summary>
/// Config connecting a display to a tracker.
/// </summary>
public class DisplayTrackerConfig
{
    #region Config Variables
    
    /// <summary>
    /// The Tracker to transform the display to.
    /// </summary>
    public VRPNTrackerConfig tracker;
    
    // Translation locks
    public bool translateX = true;
    public bool translateY = true;
    public bool translateZ = true;

    // Rotation locks
    public bool rotateX = true;
    public bool rotateY = true;
    public bool rotateZ = true;
    #endregion
    
    #region Create the Display Tracker Config

    public DisplayTrackerConfig() { }

    public void ParseConfig(SimpleJSON.JSONNode jsonNode)
    {
        // There are no translation or rotation locks
        if (jsonNode.Value != null)
        {
            tracker = PlatformConfig.current.trackers.First(i => i.id == jsonNode["id"].Value);
        }
        else
        {
            // Check for tracker a tracker
            if (jsonNode["id"] != null) tracker = PlatformConfig.current.trackers.First(i => i.id == jsonNode["id"].Value);

            // Check for translate constraints
            if (jsonNode["translateX"] != null) translateX = jsonNode["translateX"].AsBool;
            if (jsonNode["translateY"] != null) translateY = jsonNode["translateY"].AsBool;
            if (jsonNode["translateZ"] != null) translateZ = jsonNode["translateZ"].AsBool;

            // Check for rotate constraints
            if (jsonNode["rotateX"] != null) rotateX = jsonNode["rotateX"].AsBool;
            if (jsonNode["rotateY"] != null) rotateY = jsonNode["rotateY"].AsBool;
            if (jsonNode["rotateZ"] != null) rotateX = jsonNode["rotateZ"].AsBool;
        }
    }
    #endregion
}