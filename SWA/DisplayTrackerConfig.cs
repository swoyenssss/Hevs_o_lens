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
    /// </summary>
    public DisplayConfig display;

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
        // Check for tracker a tracker
        if (jsonNode["displayID"] != null) display = PlatformConfig.current.displays.First(i => i.id == jsonNode["displayID"].Value);
        if (jsonNode["trackerID"] != null) tracker = PlatformConfig.current.trackers.First(i => i.id == jsonNode["trackerID"].Value);

        // Check for translate constraints
        if (jsonNode["translateX"] != null) translateX = jsonNode["translateX"].AsBool;
        if (jsonNode["translateY"] != null) translateY = jsonNode["translateY"].AsBool;
        if (jsonNode["translateZ"] != null) translateZ = jsonNode["translateZ"].AsBool;

        // Check for rotate constraints
        if (jsonNode["rotateX"] != null) rotateX = jsonNode["rotateX"].AsBool;
        if (jsonNode["rotateY"] != null) rotateY = jsonNode["rotateY"].AsBool;
        if (jsonNode["rotateZ"] != null) rotateX = jsonNode["rotateZ"].AsBool;
    }

    public void ParseConfig(Dictionary<string, object> dictionary)
    {
        // Check for tracker a tracker
        if (dictionary.ContainsKey("displayID")) display = PlatformConfig.current.displays.First(i => i.id == (string)dictionary["displayID"]);
        if (dictionary.ContainsKey("trackerID")) tracker = PlatformConfig.current.trackers.First(i => i.id == (string)dictionary["trackerID"]);

        // Check for translate constraints
        if (dictionary.ContainsKey("translateX")) translateX = (bool)dictionary["translateX"];
        if (dictionary.ContainsKey("translateY")) translateY = (bool)dictionary["translateY"];
        if (dictionary.ContainsKey("translateZ")) translateZ = (bool)dictionary["translateZ"];

        // Check for rotate constraints
        if (dictionary.ContainsKey("rotateX")) rotateX = (bool)dictionary["rotateX"];
        if (dictionary.ContainsKey("rotateY")) rotateY = (bool)dictionary["rotateY"];
        if (dictionary.ContainsKey("rotateZ")) rotateX = (bool)dictionary["rotateZ"];
    }
    #endregion
}