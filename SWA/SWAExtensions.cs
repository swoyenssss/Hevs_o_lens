using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HEVS;

public static class SWAExtensions
{
    /// <summary>
    /// This is used because we cannot add displayTrackerConfig to a display object.
    /// </summary>
    public static DisplayTrackerConfig TrackerConfig(this DisplayConfig displayConfig)
    {
        if (SWAConfig.current.displayTrackerConfigs.ContainsKey(displayConfig))
            return SWAConfig.current.displayTrackerConfigs[displayConfig];
        return null;
    }

    /// <summary>
    /// This is used because we cannot add holoLensConfig to a display object.
    /// </summary>
    public static HoloLensConfig HoloLensData(this DisplayConfig displayConfig)
    {
        if (SWAConfig.current.holoLensConfigs.ContainsKey(displayConfig))
            return SWAConfig.current.holoLensConfigs[displayConfig];
        return null;
    }
}
