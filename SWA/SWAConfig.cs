using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HEVS;
using System.IO;
using SimpleJSON;
using UnityEngine.XR;
using UnityEngine.XR.WSA;
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
    /// Links display id's to their Display Tracker Config
    /// </summary>
    public Dictionary<DisplayConfig, DisplayTrackerConfig> displayTrackerConfigs;

    /// <summary>
    /// Links display id's to their HoloLens Config.
    /// </summary>
    public Dictionary<DisplayConfig, HoloLensConfig> holoLensConfigs;

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
        // Get the json
        var platformJSONs = GetPlatformJSONs();

        // Read in all neccessary data
        ReadDisplayTrackerConfigs(platformJSONs);
        ReadHoloPort(platformJSONs);
        ReadHoloLensConfigs(platformJSONs);

        // Set up the tracking to displays if available
        if (displayTrackerConfigs != null)
            SetUpDisplayTrackers();

        if (holoLensConfigs != null)
        {
            HoloLens.current = new HoloLens();

            // Get the if it exists on this node
            if (HoloLens.current.display != null)
            {
                var remote = HoloLens.current.display.HoloLensData().remote;

                // Set up the holoLens now or wait for remoting
                if (remote == null) HoloLens.current.Enable();
                else HolographicRemoting.Connect(remote.address, remote.maxBitRate);
            }
        }
    }

    private void Update()
    {
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
    }

    public void OnDestroy()
    {
        if (HoloLens.current != null)
            HoloLens.current.Disable();
    }

    // Neccessary for remoting.
    IEnumerator LoadDevice(string newDevice)
    {
        XRSettings.LoadDeviceByName(newDevice);
        yield return null;
        XRSettings.enabled = true;
    }
    #endregion

    #region Read JSONs

    // Reopens the config file to get the JSON data again.
    private List<JSONNode> GetPlatformJSONs()
    {
        // Grab the config file and platform
        Configuration configuration = GetComponent<Configuration>();
        
        string configFile = configuration.configFile;
        if (!File.Exists(configFile)) configFile = Path.Combine("Assets", configFile);

        string platformID = PlatformConfig.current.id;

        // Read the file again and get initial node
        StreamReader reader = new StreamReader(configFile);
        JSONNode node = JSON.Parse(reader.ReadToEnd());

        var allPlatforms = node["platforms"].Children;

        // The list to return
        List<JSONNode> platformJSONs = new List<JSONNode>();

        do
        {
            // Get the platform using the ID
            JSONNode platformJSON = allPlatforms.First(i => i["id"].Value.ToLower() == platformID);

            platformJSONs.Insert(0, platformJSON);

            // Now use the parent id
            if (platformJSON["inherited"] != null)
                platformID = platformJSON["inherited"].Value.ToLower();
            else
                platformID = null;

        } while (platformID != null);

        return platformJSONs;
    }

    // Loads the display trackers from the json
    private void ReadDisplayTrackerConfigs(List<JSONNode> platformJSONs)
    {
        // For creating the trackers
        var displayTrackerConfigs = new Dictionary<DisplayConfig, DisplayTrackerConfig>();

        // Go through the hierachy
        foreach (var platformJSON in platformJSONs)
        {
            // Check every display of the platform
            foreach (JSONNode displayJSON in platformJSON["displays"].AsArray)
            {
                JSONNode trackerJSON = displayJSON["tracker"];

                if (trackerJSON != null)
                {
                    // Get the display
                    string displayID = displayJSON["id"].Value.ToLower();
                    DisplayConfig display = PlatformConfig.current.displays.Find(i => i.id == displayID);

                    DisplayTrackerConfig displayTracker = null;

                    // Tracker needs to be updated
                    if (displayTrackerConfigs.ContainsKey(display))
                        displayTracker = displayTrackerConfigs[display];
                    else
                    {
                        // Need a new tracker
                        displayTracker = new DisplayTrackerConfig();
                        displayTrackerConfigs.Add(display, displayTracker);
                    }

                    displayTracker.ParseConfig(trackerJSON);
                }
            }
        }

        if (displayTrackerConfigs.Count > 0) { this.displayTrackerConfigs = displayTrackerConfigs; }
    }

    // Loads the HoloPort from the json
    private void ReadHoloPort(List<JSONNode> platformJSONs)
    {
        // Get the holo port if available or use 6668
        foreach (var platformJSON in platformJSONs)
        {
            JSONNode json = platformJSON["cluster"];
            if (json != null)
            {
                json = json["holo_port"];
                if (json != null) holoPort = json.AsInt;
            }
        }
    }

    // Loads the HoloLens configs form the json
    private void ReadHoloLensConfigs(List<JSONNode> platformJSONs)
    {
        // For creating the trackers
        var holoLensConfigs = new Dictionary<DisplayConfig, HoloLensConfig>();

        // Go through the hierachy
        foreach (var platformJSON in platformJSONs)
        {
            // Check every display of the platform
            foreach (JSONNode displayJSON in platformJSON["displays"].AsArray)
            {
                JSONNode typeJSON = displayJSON["type"];

                if (typeJSON != null && typeJSON.Value.ToLower() == "hololens")
                {
                    // Get the display
                    string displayID = displayJSON["id"].Value.ToLower();
                    DisplayConfig display = PlatformConfig.current.displays.Find(i => i.id == displayID);

                    HoloLensConfig holoLens = null;

                    // HoloLens needs to be updated
                    if (holoLensConfigs.ContainsKey(display))
                        holoLens = holoLensConfigs[display];
                    else
                    {
                        // Need a new tracker
                        holoLens = new HoloLensConfig();
                        holoLensConfigs.Add(display, holoLens);
                    }

                    holoLens.ParseConfig(displayJSON);
                }
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
            GameObject gameObject = new GameObject(displayTracker.Value.tracker.id + ", " + displayTracker.Key.id);
            VRPNTracker tracker = gameObject.AddComponent<VRPNTracker>();
            tracker.trackerID = displayTracker.Value.tracker.id;
            TransformDisplay display = gameObject.AddComponent<TransformDisplay>();
            display.displayIDs = new List<string>(1) { displayTracker.Key.id };
        }
    }
}
