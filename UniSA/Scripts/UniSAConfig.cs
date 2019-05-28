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

namespace HEVS.UniSA
{
    /// <summary>
    /// Adds extra configuration functionality, specifically for:
    ///     - JSON data for other classes
    ///     - Automatic tracker to displays
    ///     - HoloLens setup
    /// </summary>
    public class UniSAConfig : MonoBehaviour
    {

        #region Variables
        /// <summary>
        /// The main extra config being used by all other classes.
        /// </summary>
        public static UniSAConfig current;

        /// <summary>
        /// </summary>
        public List<DynamicDisplayConfig> dynamicDisplayConfigs;

        /// <summary>
        /// </summary>
        public List<DynamicCameraConfig> dynamicCameraConfigs;
        
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
            ReadDynamicDisplayConfigs();
            ReadDynamicCameraConfigs();
            ReadHoloLensConfigs();
 
            #region Set Up Dynamic Displays/Cameras

            // Get the Holoport if it exists
            if (PlatformConfig.current.globals.ContainsKey("holo_port"))
                holoPort = (int)PlatformConfig.current.globals["holo_port"];

            // For the tracking
            var trackerById = new Dictionary<string, VRPNTracker>();
            Transform trackerParent = new GameObject("Trackers").transform;
            trackerParent.transform.parent = Camera.main.transform.parent;

            // For every dynamic camera config
            foreach (var config in dynamicCameraConfigs)
                CreateDynamicCamera(trackerById, config, trackerParent);

            // For every dynamic display config
            foreach (var config in dynamicDisplayConfigs)
                CreateDynamicDisplay(trackerById, config, trackerParent);

            if (trackerParent.childCount == 0) { Destroy(trackerParent); }
            #endregion

            #region Set Up HoloLens

            // Only create singleton if it will be used
            if (holoLensConfigs != null)
            {
                // Master must have a holoLens object
                if (Cluster.isMaster) HoloLens.current = new HoloLens();

                // Client must have a holoLens device
                foreach (DisplayConfig display in NodeConfig.current.displays)
                {
                    if (holoLensConfigs.ContainsKey(display))
                    {
                        HoloLensDevice.current = new HoloLensDevice(display);
                        break;
                    }
                }
            }
            #endregion
        }

        private void Update()
        {
            if (HoloLensDevice.current != null) HoloLensDevice.current.Update();
        }

        private void OnDestroy()
        {
            if (HoloLensDevice.current != null) HoloLensDevice.current.Disable();
            if (HoloLens.current != null) HoloLens.current.Disable();
        }
        #endregion

        #region Read in Configs

        // Reads dynamic display configs from globals.
        private void ReadDynamicDisplayConfigs()
        {
            // For creating the trackers
            dynamicDisplayConfigs = new List<DynamicDisplayConfig>();

            // No need to continue if there are no display trackers
            if (!PlatformConfig.current.globals.ContainsKey("dynamic_displays")) { return; }

            foreach (var dictionary in (object[])PlatformConfig.current.globals["dynamic_displays"])
            {
                DynamicDisplayConfig config = new DynamicDisplayConfig();
                config.Parse((Dictionary<string, object>)dictionary);
                dynamicDisplayConfigs.Add(config);
            }
        }

        // Reads dynamic camera configs from globals.
        private void ReadDynamicCameraConfigs()
        {
            // For creating the trackers
            dynamicCameraConfigs = new List<DynamicCameraConfig>();

            // No need to continue if there are no display trackers
            if (!PlatformConfig.current.globals.ContainsKey("dynamic_cameras")) { return; }

            foreach (var dictionary in (object[])PlatformConfig.current.globals["dynamic_cameras"])
            {
                DynamicCameraConfig config = new DynamicCameraConfig();
                config.Parse((Dictionary<string, object>)dictionary);
                dynamicCameraConfigs.Add(config);
            }
        }

        // Loads the HoloLens configs fromm the json
        private void ReadHoloLensConfigs()
        {
            // For creating the trackers
            var holoLensConfigs = new Dictionary<DisplayConfig, HoloLensData>();

            // Check every display of the platform
            foreach (DisplayConfig display in PlatformConfig.current.displays)
            {
                if (display.json == null) { continue; }
                JSONNode typeJSON = display.json["type"];

                if (typeJSON != null && typeJSON.Value.ToLower() == "hololens")
                {
                    HoloLensData holoLens = new HoloLensData();
                    holoLensConfigs.Add(display, holoLens);

                    // TODO: Should not have to load scale
                    if (display.json["transform"] != null && display.json["transform"]["scale"] != null)
                    {
                        var transformJSON = display.json["transform"];

                        if (transformJSON["scale"] != null)
                        {
                            var scaleJSON = transformJSON["scale"].AsArray;
                            display.transform.scale = new Vector3(scaleJSON[0].AsFloat, scaleJSON[1].AsFloat, scaleJSON[2].AsFloat);
                        }
                    }

                    holoLens.ParseJson(display.json);
                }
            }

            if (holoLensConfigs.Count > 0) { this.holoLensConfigs = holoLensConfigs; }
        }
        #endregion

        #region Dynamic Displays/Cameras
        
        // Modifies the camera to be dynamic
        private void CreateDynamicCamera(Dictionary<string, VRPNTracker> trackerById, DynamicCameraConfig config, Transform parent)
        {
            UnityEngine.Camera camera = config.display.Camera();
            if (camera == null) { return; }

            // Use a tracker if needed
            if (config.tracker != null)
            {
                VRPNTracker tracker = GetTracker(trackerById, config.tracker, parent);
                camera.transform.parent = tracker.gameObject.transform;
            }

            // TODO: This may not work properly if there is only one camera
            camera.transform.localPosition = config.transform.translate;
            camera.transform.localRotation = config.transform.rotate;
            camera.transform.localScale = config.transform.scale;

            // Create dynamic display
            DynamicCamera dynamicCamera = camera.gameObject.AddComponent<DynamicCamera>();
            dynamicCamera.disableIfNotFound = false;
            dynamicCamera.displayID = config.display.id;
            dynamicCamera.cullInfront = config.cullInfront;
        }

        // Creates a dynamic display
        private void CreateDynamicDisplay(Dictionary<string, VRPNTracker> trackerById, DynamicDisplayConfig config, Transform parent)
        {
            VRPNTracker tracker = GetTracker(trackerById, config.tracker, parent);

            // Create dynamic display
            DynamicDisplay dynamicDisplay = new GameObject(config.display.id + "-DynamicDisplay").AddComponent<DynamicDisplay>();
            dynamicDisplay.transform.parent = tracker.transform;
            dynamicDisplay.displayID = config.display.id;

            // Set the default transform
            dynamicDisplay.transform.localPosition = config.display.transform.translate;
            dynamicDisplay.transform.localRotation = config.display.transform.rotate;
            dynamicDisplay.transform.localScale = config.display.transform.scale;
        }

        private VRPNTracker GetTracker(Dictionary<string, VRPNTracker> trackerById, VRPNTrackerConfig config, Transform parent)
        {
            // Use tracker if it already exists
            if (trackerById.ContainsKey(config.id)) return trackerById[config.id];

            // Otherwise create new tracker
            VRPNTracker tracker = new GameObject(config.id + "-Tracker").AddComponent<VRPNTracker>();
            tracker.transform.parent = parent;
            trackerById.Add(config.id, tracker);

            // Set up the tracker
            tracker.trackerID = config.id;
            tracker.applyRotation = config.applyRotation;
            tracker.deltaTransform = config.deltaTransform;
            tracker.forward = config.forward;
            tracker.right = config.right;
            tracker.up = config.up;

            return tracker;
        }
        #endregion
    }
}