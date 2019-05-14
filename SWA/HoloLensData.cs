using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HEVS;
using UnityEngine.XR.WSA;
using System.Linq;
using SimpleJSON;
using System;

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
    /// The scale of the HoloLens to HEVS.
    /// </summary>
    public float scale = 1f;

    /// <summary>
    /// The information for holographic remoting.
    /// </summary>
    public Remote remote;

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
        // Set the start type
        if (json["origin"] != null)
        {
            switch (json["origin"].Value)
            {
                case "starting_location":
                    origin = OriginType.START_LOCATION;
                    break;
            }
        }

        // Get the world scale
        if (json["scale"] != null) { scale = json["scale"].AsFloat; }

        // Get the world scale
        if (json["remote"] != null)
        {
            var remoteJSON = json["remote"];

            // Create the remote if it did not already exist
            if (remote == null) { remote = new Remote(); }

            // Set remote variables
            if (remoteJSON["address"] != null) { remote.address = remoteJSON["address"].Value; }
            if (remoteJSON["max_bit_rate"] != null) { remote.maxBitRate = remoteJSON["max_bit_rate"].AsInt; }
        }

        // Set the button gestures
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

        // Set the axes gestures
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
                Debug.Log(gesture.mappingsX[0]);
                gesture.mappingsY = GetMappings(gestureJSON["mapping_y"]);
                Debug.Log(gesture.mappingsY[0]);
                gesture.mappingsZ = GetMappings(gestureJSON["mapping_z"]);
                Debug.Log(gesture.mappingsZ[0]);
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
        foreach (var mapping in mappingJSON.Children)
            mappings.Add(mapping.Value);

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