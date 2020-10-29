using System.Collections.Generic;
using SimpleJSON;

namespace HEVS.UniSA
{
    /// <summary>
    /// Config connecting a display to a tracker.
    /// </summary>
    public class DynamicDisplayConfig : JSONConfigObject
    {
        #region Config Variables

        /// <summary>
        /// The unique id of the dynamic camera config
        /// </summary>
        public string id;

        /// <summary>
        /// The display to be transformed.
        /// </summary>
        public DisplayConfig display;

        /// <summary>
        /// The tracker that will transform the display.
        /// </summary>
        public TrackerConfig tracker;

        /// <summary>
        /// If objects infront of the camera should be visible;
        /// </summary>
        public bool cullInfront = false;
        #endregion

        #region Create the Dynamic Display Config

        public DynamicDisplayConfig() { }

        public object Clone()
        {
            DynamicDisplayConfig clone = new DynamicDisplayConfig();

            clone.id = id;
            clone.display = display;
            clone.tracker = tracker;

            return clone;
        }

        /// <summary>
        /// Parses the config from a json.
        /// </summary>
        /// <param name="jsonNode">The dynamic display json.</param>
        public bool Parse(JSONNode jsonNode)
        {
            if (jsonNode["id"] != null) id = jsonNode["id"].Value;
            if (jsonNode["display"] != null) display = PlatformConfig.current.displays.Find(i => i.id == jsonNode["display"].Value);
            if (jsonNode["tracker"] != null) tracker = PlatformConfig.current.trackers.Find(i => i.id == jsonNode["tracker"].Value);
            return true;
        }

        /// <summary>
        /// Parses the config from a dictionary.
        /// </summary>
        /// <param name="jsonNode">The dynamic display as a dictionary.</param>
        public bool Parse(Dictionary<string, object> dictionary)
        {
            if (dictionary.ContainsKey("id")) id = (string)dictionary["id"];
            if (dictionary.ContainsKey("display")) display = PlatformConfig.current.displays.Find(i => i.id == (string)dictionary["display"]);
            if (dictionary.ContainsKey("tracker")) tracker = PlatformConfig.current.trackers.Find(i => i.id == (string)dictionary["tracker"]);
            return true;
        }

        bool JSONConfigObject.Parse(JSONNode json)
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}