using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HEVS;

public static class SWAExtensions
{
    /// <summary>
    /// This is used because we cannot add holoLensConfig to a display object.
    /// </summary>
    public static HoloLensData HoloLensData(this DisplayConfig displayConfig)
    {
        if (SWAConfig.current.holoLensConfigs.ContainsKey(displayConfig))
            return SWAConfig.current.holoLensConfigs[displayConfig];
        return null;
    }
}
