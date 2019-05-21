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
    /// The display to be transformed.
    /// </summary>
    public DisplayConfig display;

    /// <summary>
    /// The tracker that will transform the display.
    /// </summary>
    public VRPNTrackerConfig tracker;

    #endregion

    #region Create the Display Tracker Config

    public DisplayTrackerConfig() { }

    /// <summary>
    /// Parses the config from a json.
    /// </summary>
    /// <param name="jsonNode">The displayTracker json.</param>
    public void ParseConfig(SimpleJSON.JSONNode jsonNode)
    {
        // Check for tracker a tracker
        if (jsonNode["displayID"] != null) display = PlatformConfig.current.displays.First(i => i.id == jsonNode["displayID"].Value);
        if (jsonNode["trackerID"] != null) tracker = PlatformConfig.current.trackers.First(i => i.id == jsonNode["trackerID"].Value);
    }

    /// <summary>
    /// Parses the config from a dictionary.
    /// </summary>
    /// <param name="jsonNode">The displayTracker as a dictionary.</param>
    public void ParseConfig(Dictionary<string, object> dictionary)
    {
        // Check for tracker a tracker
        if (dictionary.ContainsKey("displayID")) display = PlatformConfig.current.displays.First(i => i.id == (string)dictionary["displayID"]);
        if (dictionary.ContainsKey("trackerID")) tracker = PlatformConfig.current.trackers.First(i => i.id == (string)dictionary["trackerID"]);
    }
    #endregion
}