using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace HEVS.UniSA
{
    /// <summary>
    /// Config connecting a display to a tracker.
    /// </summary>
    public class DynamicDisplayConfig
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

        /// <summary>
        /// If objects infront of the camera should be visible;
        /// </summary>
        public bool cullInfront = false;
        #endregion

        #region Create the Dynamic Display Config

        public DynamicDisplayConfig() { }

        /// <summary>
        /// Parses the config from a json.
        /// </summary>
        /// <param name="jsonNode">The dynamic display json.</param>
        public void Parse(JSONNode jsonNode)
        {
            if (jsonNode["display"] != null) display = PlatformConfig.current.displays.Find(i => i.id == jsonNode["display"].Value);
            if (jsonNode["tracker"] != null) tracker = PlatformConfig.current.trackers.Find(i => i.id == jsonNode["tracker"].Value);
        }

        /// <summary>
        /// Parses the config from a dictionary.
        /// </summary>
        /// <param name="jsonNode">The dynamic display as a dictionary.</param>
        public void Parse(Dictionary<string, object> dictionary)
        {
            // Check for tracker a tracker
            if (dictionary.ContainsKey("display")) display = PlatformConfig.current.displays.Find(i => i.id == (string)dictionary["display"]);
            if (dictionary.ContainsKey("tracker")) tracker = PlatformConfig.current.trackers.Find(i => i.id == (string)dictionary["tracker"]);
        }
        #endregion
    }
}