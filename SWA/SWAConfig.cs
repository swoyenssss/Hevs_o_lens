using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HEVS;
using System.IO;
using SimpleJSON;
#if UNITY_WSA
using UnityEngine.XR;
using UnityEngine.XR.WSA;
#endif
using System;

// TODO: This does not use inheritence

/// <summary>
/// Adds extra configuration functionality, specifically for:
///     - JSON data for other classes
///     - Automatic tracker to displays
///     - HoloLens setup
/// </summary>
public class SWAConfig : MonoBehaviour
{

    #region Variables
    /// <summary>
    /// The main extra config being used by all other classes.
    /// </summary>
    public static SWAConfig current;

    /// <summary>
    /// </summary>
    public List<DisplayTrackerConfig> displayTrackerConfigs;

    /// <summary>
    /// Links display id's to their HoloLens Config.
    /// </summary>
    public Dictionary<DisplayConfig, HoloLensData> holoLensConfigs;

    /// <summary>
    /// The port for sending holoLens 6Dof
    /// </summary>
    [HideInInspector]
    public int holoPort = 6668;

    #endregion

    #region Unity Methods

    private void Start()
    {
        current = this;

        // Read in all neccessary data
        ReadDisplayTrackerConfigs();
        ReadHoloLensConfigs();

        // Get the Holoport if it exists
        if (PlatformConfig.current.globals.ContainsKey("holoPort"))
            holoPort = (int)PlatformConfig.current.globals["holoPort"];

        // Set up the tracking to displays if available
        if (displayTrackerConfigs != null)
            SetUpDisplayTrackers();

        if (holoLensConfigs != null)
        {
            // Only create singleton if it will be used
            HoloLens.current = new HoloLens();

#if UNITY_WSA
            // Get the device it exists on this node
            if (HoloLens.current.display != null)
            {
                var remote = HoloLens.current.display.HoloLensData().remote;

                // Set up the holoLens now or wait for remoting
                if (remote == null) HoloLens.current.Enable();
                else HolographicRemoting.Connect(remote.address, remote.maxBitRate);
            }
#endif
        }
    }

    private void Update()
    {
#if UNITY_WSA
        if (HoloLens.current != null && HoloLens.current.display != null)
        {
            var remote = HoloLens.current.display.HoloLensData().remote;

            // If remoting has now connected
            if (remote != null && remote.connected == false
                && HolographicRemoting.ConnectionState == HolographicStreamerConnectionState.Connected)
            {
                remote.connected = true;
                StartCoroutine(LoadDevice("WindowsMR"));
                HoloLens.current.Enable();
            }

            // TODO: Would rather not send data like this.
            if (remote == null || remote.connected == true) HoloLens.current.SendTransformData();
        }
#endif
        
        Debug.Log(HEVS.Input.GetAxis("Horizontal") + ", " + HEVS.Input.GetAxis("Depth") + ", " + HEVS.Input.GetAxis("Vertical"));
    }

    public void OnDestroy()
    {
        if (HoloLens.current != null)
            HoloLens.current.Disable();
    }

#if UNITY_WSA
    // Neccessary for remoting.
    IEnumerator LoadDevice(string newDevice)
    {
        XRSettings.LoadDeviceByName(newDevice);
        yield return null;
        XRSettings.enabled = true;
    }
#endif
    #endregion

    #region Read JSONs
    
    // Loads the display trackers from the json
    private void ReadDisplayTrackerConfigs()
    {
        // For creating the trackers
        displayTrackerConfigs = new List<DisplayTrackerConfig>();

        // No need to continue if there are no display trackers
        if (!PlatformConfig.current.globals.ContainsKey("displayTrackers")) { return; }

        foreach (var dictionary in (Dictionary<string, object>[])PlatformConfig.current.globals["displayTrackers"])
        {
            DisplayTrackerConfig config = new DisplayTrackerConfig();
            config.ParseConfig(dictionary);
            displayTrackerConfigs.Add(config);
        }
    }

    // Loads the HoloLens configs form the json
    private void ReadHoloLensConfigs()
    {
        // For creating the trackers
        var holoLensConfigs = new Dictionary<DisplayConfig, HoloLensData>();

        // Check every display of the platform
        foreach (DisplayConfig display in PlatformConfig.current.displays)
        {
            JSONNode typeJSON = display.json["type"];

            if (typeJSON != null && typeJSON.Value.ToLower() == "hololens")
            {
                HoloLensData holoLens = new HoloLensData();
                holoLensConfigs.Add(display, holoLens);

                holoLens.ParseJson(display.json);
            }
        }

        if (holoLensConfigs.Count > 0) { this.holoLensConfigs = holoLensConfigs; }
    }
    #endregion

    // Adds Trackers and TransfromDisplays to new GameObjects
    private void SetUpDisplayTrackers()
    {
        foreach (var displayTracker in displayTrackerConfigs)
        {
            GameObject gameObject = new GameObject(displayTracker.tracker.id + ", " + displayTracker.display.id);
            VRPNTracker tracker = gameObject.AddComponent<VRPNTracker>();
            tracker.trackerID = displayTracker.tracker.id;
            TransformDisplay display = gameObject.AddComponent<TransformDisplay>();
            display.displayIDs = new List<string>(1) { displayTracker.display.id };
        }
    }
}
