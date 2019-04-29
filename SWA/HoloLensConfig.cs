using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HEVS;
using UnityEngine.XR.WSA;
using System.Linq;

/// <summary>
/// Config for a HoloLens display type.
/// </summary>
public class HoloLensConfig
{
    #region Config Variables 

    /// <summary>
    /// How the HoloLens to HEVS origin is set.
    /// </summary>
    public OriginType origin;

    /// <summary>
    /// The scale of the HoloLens to HEVS.
    /// </summary>
    public float scale;

    /// <summary>
    /// The information for holographic remoting.
    /// </summary>
    public Remote remote;

    /// <summary>
    /// The gestures to use from the HoloLens.
    /// </summary>
    public List<Gesture> gestures = new List<Gesture>();
    
    #endregion

    #region Create the HoloLens Config

    public HoloLensConfig() { }

    public void ParseConfig(SimpleJSON.JSONNode jsonNode)
    {
        // Set the start type
        if (jsonNode["origin"] != null)
        {
            switch (jsonNode["origin"].Value)
            {
                case "starting_location":
                    origin = OriginType.START_LOCATION;
                    break;
            }
        }

        // Get the world scale
        if (jsonNode["scale"] != null) { scale = jsonNode["scale"].AsFloat; }

        // Get the world scale
        if (jsonNode["remote"] != null)
        {
            var remoteJSON = jsonNode["remote"];

            // Create the remote if it did not already exist
            if (remote == null) { remote = new Remote(); }

            // Set remote variables
            if (remoteJSON["address"] != null) { remote.address = remoteJSON["address"].Value; }
            if (remoteJSON["max_bit_rate"] != null) { remote.maxBitRate = remoteJSON["max_bit_rate"].AsInt; }
        }

        // Set the gestures
        if (jsonNode["gestures"] != null)
        {
            foreach (var gestureJSON in jsonNode["gestures"].Children)
            {
                // Get existing gesture
                Gesture gesture = new Gesture(); // TODO: dont repeat gestures
                gestures.Add(gesture);

                // Set the type
                switch (gestureJSON["type"].Value)
                {
                    case "tap":
                        gesture.type = GestureType.TAP;
                        break;

                    case "double tap":
                        gesture.type = GestureType.DOUBLE_TAP;
                        break;

                    case "hold":
                        gesture.type = GestureType.HOLD;
                        break;
                }

                var mappingJSON = gestureJSON["mapping"];
                if (mappingJSON.Value != null)
                {
                    // Only the one mapping
                    gesture.mappings = new List<string>(1) { mappingJSON.Value };
                }
                else
                {
                    // Create new list of mappings
                    gesture.mappings = new List<string>(mappingJSON.Count);

                    // Get the mappings
                    foreach (var mapping in mappingJSON.Children)
                        gesture.mappings.Add(mapping.Value);
                }
            }
        }
    }
    #endregion
    
    #region Enums and Other Classes

    /// <summary>
    /// Used to represent a gesture as input for a HoloLens
    /// </summary>
    public class Gesture
    {
        /// <summary>
        /// The type of gesture.
        /// </summary>
        public GestureType type;

        /// <summary>
        /// The input mappings for the gestures
        /// </summary>
        public List<string> mappings;
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

    public enum GestureType {
        TAP,
        DOUBLE_TAP,
        HOLD
    }
    #endregion
}