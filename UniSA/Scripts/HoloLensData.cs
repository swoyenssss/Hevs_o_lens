using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HEVS;
using UnityEngine.XR.WSA;
using System.Linq;
using SimpleJSON;
using System;

namespace HEVS.UniSA
{

    /// <summary>
    /// Config for a HoloLens display type.
    /// </summary>
    public class HoloLensData : DisplayData
    {
        #region Config Variables 

        /// <summary>
        /// How the HoloLens to HEVS origin is set.
        /// </summary>
        public OriginType origin;

        /// <summary>
        /// The information for holographic remoting.
        /// </summary>
        public Remote remote;

        /// <summary>
        /// Prevents holoLens from seeing through these displays.
        /// </summary>
        public List<DisplayConfig> cullDisplays = new List<DisplayConfig>();

        /// <summary>
        /// Draw these displays on holoLens.
        /// </summary>
        public List<DisplayConfig> drawDisplays = new List<DisplayConfig>();

        /// <summary>
        /// The gestures to use from the HoloLens.
        /// </summary>
        public List<ButtonGesture> buttonGestures = new List<ButtonGesture>();

        /// <summary>
        /// The gestures to use from the HoloLens.
        /// </summary>
        public List<AxesGesture> axesGestures = new List<AxesGesture>();

        public DisplayType type { get { return DisplayType.UserDefined; } }

        #endregion

        #region Create the HoloLens Config

        public HoloLensData() { }

        public void ParseJson(JSONNode json)
        {
            if (json["origin"] != null)
            {
                switch (json["origin"].Value)
                {
                    case "start_location":
                        origin = OriginType.START_LOCATION;
                        break;
                }
            }

            if (json["remote"] != null)
            {
                var remoteJSON = json["remote"];

                // Create the remote if it did not already exist
                if (remote == null) { remote = new Remote(); }

                // Set remote variables
                if (remoteJSON["address"] != null) { remote.address = remoteJSON["address"].Value; }
                if (remoteJSON["max_bit_rate"] != null) { remote.maxBitRate = remoteJSON["max_bit_rate"].AsInt; }
            }

            if (json["cull_displays"] != null)
            {
                var cullDisplaysJSON = json["cull_displays"].AsArray;

                // Add each cull display
                foreach (var display in cullDisplaysJSON.Children)
                    cullDisplays.Add(PlatformConfig.current.displays.Find(i => i.id == display.Value));
            }

            if (json["draw_displays"] != null)
            {
                var drawDisplaysJSON = json["draw_displays"].AsArray;
                
                // Add each cull display
                foreach (var display in drawDisplaysJSON.Children)
                    drawDisplays.Add(PlatformConfig.current.displays.Find(i => i.id == display.Value));
            }

            if (json["button_gestures"] != null)
            {
                foreach (var gestureJSON in json["button_gestures"].Children)
                {
                    // Get existing gesture
                    ButtonGesture gesture = new ButtonGesture(); // TODO: dont repeat gestures
                    buttonGestures.Add(gesture);

                    // Set the type
                    switch (gestureJSON["type"].Value)
                    {
                        case "tap":
                            gesture.type = ButtonGesture.Type.TAP;
                            break;

                        case "double_tap":
                            gesture.type = ButtonGesture.Type.DOUBLE_TAP;
                            break;

                        case "hold":
                            gesture.type = ButtonGesture.Type.HOLD;
                            break;
                    }

                    gesture.mappings = GetMappings(gestureJSON["mapping"]);
                }
            }

            if (json["axes_gestures"] != null)
            {
                foreach (var gestureJSON in json["axes_gestures"].Children)
                {
                    // Get existing gesture
                    AxesGesture gesture = new AxesGesture(); // TODO: dont repeat gestures
                    axesGestures.Add(gesture);

                    // Set the type
                    switch (gestureJSON["type"].Value)
                    {
                        case "manipulation":
                            gesture.type = AxesGesture.Type.MANIPULATION;
                            break;

                        case "navigation":
                            gesture.type = AxesGesture.Type.NAVIGATION;
                            break;
                    }

                    gesture.mappingsX = GetMappings(gestureJSON["mapping_x"]);
                    gesture.mappingsY = GetMappings(gestureJSON["mapping_y"]);
                    gesture.mappingsZ = GetMappings(gestureJSON["mapping_z"]);
                }
            }
        }

        private List<string> GetMappings(JSONNode mappingJSON)
        {
            // Only the one mapping
            if (!string.IsNullOrEmpty(mappingJSON.Value))
                return new List<string>(1) { mappingJSON.Value };

            // Create new list of mappings
            var mappings = new List<string>(mappingJSON.Count);

            // Get the mappings
            foreach (var mapping in mappingJSON.AsArray)
                mappings.Add((string)mapping);

            return mappings;
        }

        public void Clone(DisplayData original)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        #region Enums and Other Classes

        /// <summary>
        /// Used to represent a gesture as input for a HoloLens
        /// </summary>
        public class ButtonGesture
        {
            /// <summary>
            /// The type of gesture.
            /// </summary>
            public Type type;

            public bool performed;

            /// <summary>
            /// The input mappings for the gestures
            /// </summary>
            public List<string> mappings;

            public enum Type
            {
                TAP,
                DOUBLE_TAP,
                HOLD
            }
        }
        /// <summary>
        /// </summary>
        public class AxesGesture
        {
            /// <summary>
            /// </summary>
            public Type type;

            public Vector3 axes;

            /// <summary>
            /// </summary>
            public List<string> mappingsX;

            /// <summary>
            /// </summary>
            public List<string> mappingsY;

            /// <summary>
            /// </summary>
            public List<string> mappingsZ;

            public enum Type
            {
                MANIPULATION,
                NAVIGATION
            }
        }

        /// <summary>
        /// Used for remote accessing a HoloLens
        /// </summary>
        public class Remote
        {
            /// <summary>
            /// The address of the HoloLens.
            /// </summary>
            public string address;

            /// <summary>
            /// The maximum bit rate for sending data.
            /// </summary>
            public int maxBitRate = 9999;

            /// <summary>
            /// Is the holoLens connected
            /// </summary>
            public bool connected = false;
        }

        public enum OriginType
        {
            START_LOCATION,
            CHOOSE_ORIGIN,
            FIND_ORIGIN
        }


        internal void ParseConfig(JSONNode json)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}