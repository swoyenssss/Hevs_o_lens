using System.Collections.Generic;
using UnityEngine;

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
        #endregion

        #region Unity Methods

        private void Start()
        {
            current = this;
            
            // Read in all neccessary data
            ReadDynamicDisplayConfigs();
            ReadDynamicCameraConfigs();
            
            // For the tracking
            var trackerById = new Dictionary<string, TrackerManager>();
            Transform trackerParent = new GameObject("Trackers").transform;
            trackerParent.transform.parent = Camera.main.transform.parent;

            // For every dynamic camera config
            foreach (var config in dynamicCameraConfigs)
                CreateDynamicCamera(trackerById, config, trackerParent);

            // For every dynamic display config
            foreach (var config in dynamicDisplayConfigs)
                CreateDynamicDisplay(trackerById, config, trackerParent);

            // Don't need the tracker object if it has no trackers
            if (trackerParent.childCount == 0) { Destroy(trackerParent.gameObject); }
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
        #endregion

        #region Dynamic Displays/Cameras
        
        // Modifies the camera to be dynamic
        private void CreateDynamicCamera(Dictionary<string, TrackerManager> trackerById, DynamicCameraConfig config, Transform parent)
        {
            Transform displayTransform = config.display.Transform();

            // Use a tracker if needed
            if (config.tracker != null)
            {
                TrackerManager tracker = GetTracker(trackerById, config.tracker, parent);
                displayTransform.parent = tracker.gameObject.transform;
            }
            
            config.display.cameraParent = displayTransform;

            // Update to have configs transforms
            displayTransform.localPosition = config.transform.translate;
            displayTransform.localRotation = config.transform.rotate;
            displayTransform.localScale = config.transform.scale;

            // Create dynamic display
            DynamicCamera dynamicCamera = displayTransform.gameObject.AddComponent<DynamicCamera>();
            dynamicCamera.disableIfNotFound = false;
            dynamicCamera.displayID = config.display.id;
            dynamicCamera.cullInfront = config.cullInfront;
        }

        // Creates a dynamic display
        private void CreateDynamicDisplay(Dictionary<string, TrackerManager> trackerById, DynamicDisplayConfig config, Transform parent)
        {
            TrackerManager tracker = GetTracker(trackerById, config.tracker, parent);

            // Create dynamic display
            DynamicDisplay dynamicDisplay = new GameObject(config.display.id + "-DynamicDisplay").AddComponent<DynamicDisplay>();
            dynamicDisplay.transform.parent = tracker.transform;
            dynamicDisplay.displayID = config.display.id;

            // Set the default transform
            dynamicDisplay.transform.localPosition = config.display.transform.translate;
            dynamicDisplay.transform.localRotation = config.display.transform.rotate;
            dynamicDisplay.transform.localScale = config.display.transform.scale;
        }

        private TrackerManager GetTracker(Dictionary<string, TrackerManager> trackerById, TrackerConfig config, Transform parent)
        {
            // Use tracker if it already exists
            if (trackerById.ContainsKey(config.id)) return trackerById[config.id];

            // Otherwise create new tracker
            TrackerManager tracker = new GameObject(config.id + "-Tracker").AddComponent<TrackerManager>();
            tracker.transform.parent = parent;
            trackerById.Add(config.id, tracker);
            tracker.id = config.id;

            return tracker;
        }
        #endregion
    }
}