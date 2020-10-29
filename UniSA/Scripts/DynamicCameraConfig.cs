﻿using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;


namespace HEVS.UniSA
{

    public class DynamicCameraConfig : JSONConfigObject
    {
        /// <summary>
        /// The unique id of the dynamic camera config
        /// </summary>
        public string id;

        /// <summary>
        /// The display to be transformed.
        /// </summary>
        public DisplayConfig display;

        /// <summary>
        /// 
        /// </summary>
        public TransformConfig transform;

        /// <summary>
        /// The tracker that will transform the camera.
        /// </summary>
        public TrackerConfig tracker;

        /// <summary>
        /// If objects infront of the camera should be visible;
        /// </summary>
        public bool cullInfront = false;

        public DynamicCameraConfig() { }

        public object Clone()
        {
            DynamicCameraConfig clone = new DynamicCameraConfig();

            clone.id = id;
            clone.display = display;
            clone.tracker = tracker;
            clone.transform = (TransformConfig)transform.Clone();

            return clone;
        }

        /// <summary>
        /// Parses the config from a json.
        /// </summary>
        /// <param name="jsonNode">The dynamic display json.</param>
        public bool Parse(JSONNode jsonNode)
        {
            if (jsonNode["id"] != null) id = jsonNode["id"].Value; 
            if (jsonNode["tracker"] != null) tracker = PlatformConfig.current.trackers.Find(i => i.id == jsonNode["tracker"].Value);
            if (jsonNode["display"] != null) display = PlatformConfig.current.displays.Find(i => i.id == jsonNode["display"].Value);
            if (jsonNode["cull_infront"] != null) cullInfront = jsonNode["cull_infront"].AsBool;
            if (jsonNode["transform"] != null) transform.Parse(jsonNode["transform"]);

            return true;
        }

        /// <summary>
        /// Parses the config from a dictionary.
        /// </summary>
        /// <param name="jsonNode">The dynamic display as a dictionary.</param>
        public void Parse(Dictionary<string, object> dictionary)
        {
            if (dictionary.ContainsKey("id")) id = (string)dictionary["id"];
            if (dictionary.ContainsKey("display")) display = PlatformConfig.current.displays.Find(i => i.id == (string)dictionary["display"]);
            if (dictionary.ContainsKey("tracker")) tracker = PlatformConfig.current.trackers.Find(i => i.id == (string)dictionary["tracker"]);
            if (dictionary.ContainsKey("cull_infront")) cullInfront = (bool)dictionary["cull_infront"];

            // Get the transform data
            if (dictionary.ContainsKey("transform"))
            {
                Dictionary<string, object> transformDic = (Dictionary<string, object>)dictionary["transform"];

                // TODO: Float conversion is bad
                if (transformDic.ContainsKey("translate"))
                {
                    object[] translateArray = (object[])transformDic["translate"];
                    transform.translate = new Vector3(float.Parse(translateArray[0].ToString()), float.Parse(translateArray[1].ToString()), float.Parse(translateArray[2].ToString()));
                }

                if (transformDic.ContainsKey("rotate"))
                {
                    object[] rotateArray = (object[])transformDic["rotate"];
                    transform.rotate = Quaternion.Euler(float.Parse(rotateArray[0].ToString()), float.Parse(rotateArray[1].ToString()), float.Parse(rotateArray[2].ToString()));
                }

                if (transformDic.ContainsKey("scale"))
                {
                    object[] scaleArray = (object[])transformDic["scale"];
                    transform.scale = new Vector3(float.Parse(scaleArray[0].ToString()), float.Parse(scaleArray[1].ToString()), float.Parse(scaleArray[2].ToString()));
                }
            }
        }
    }
}