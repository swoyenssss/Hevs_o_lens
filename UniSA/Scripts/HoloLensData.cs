using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

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
        /// If the world anchor should be synchronised with other HoloLenses.
        /// </summary>
        public bool shareOrigin = true;

        /// <summary>
        /// Should a stored world anchor be loaded if it exists.
        /// </summary>
        public bool usePreviousOrigin = false;

        /// <summary>
        /// If the background should be hidden.
        /// </summary>
        public bool disableBackground = true;

        /// <summary>
        /// If real world objects should cull.
        /// </summary>
        public bool cullMesh = false;

        /// <summary>
        /// The clipping plane for the HoloLens' camera.
        /// </summary>
        public float clippingPlane = 0.85f;

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
        /// The tracker to be used as the holoLens.
        /// </summary>
        public TrackerConfig tracker;

        /// <summary>
        /// The tracker to be used as the left hand.
        /// </summary>
        public TrackerConfig leftHandTracker;

        /// <summary>
        /// The tracker to be used as the right.
        /// </summary>
        public TrackerConfig rightHandTracker;

        /// <summary>
        /// The tracker to be used as the curser.
        /// </summary>
        public TrackerConfig cursorTracker;

        /// <summary>
        /// The display type.
        /// </summary>
        public DisplayType type { get { return DisplayType.UserDefined; } }
        #endregion

        #region Create the HoloLens Config

        /// <summary>
        /// Constructs an empty holoLens data object.
        /// </summary>
        public HoloLensData() { }

        /// <summary>
        /// Parses json data into the holoLens data.
        /// </summary>
        /// <param name="json">JSONNode of the display.</param>
        public void ParseJson(JSONNode json)
        {
            if (json["clipping_plane"] != null) clippingPlane = json["clipping_plane"].AsFloat;
            if (json["disable_background"] != null) disableBackground = json["disable_background"].AsBool;
            if (json["share_origin"] != null) shareOrigin = json["share_origin"].AsBool;
            if (json["use_previous_origin"] != null) usePreviousOrigin = json["use_previous_origin"].AsBool;
            if (json["cull_mesh"] != null) cullMesh = json["cull_mesh"].AsBool;

            // Choose how the origin is set
            if (json["origin"] != null)
            {
                switch (json["origin"].Value)
                {
                    case "StartLocation":
                        origin = OriginType.START_LOCATION;
                        break;

                    case "ChooseOrigin":
                        origin = OriginType.CHOOSE_ORIGIN;
                        break;

                    case "FindOrigin":
                        origin = OriginType.FIND_MARKER;
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

            // Get all the relevant trackers
            if (json["tracker"] != null) tracker = PlatformConfig.current.trackers.Find(i => i.id == json["tracker"].Value);
            if (json["left_hand_tracker"] != null) leftHandTracker = PlatformConfig.current.trackers.Find(i => i.id == json["left_hand_tracker"].Value);
            if (json["right_hand_tracker"] != null) rightHandTracker = PlatformConfig.current.trackers.Find(i => i.id == json["right_hand_tracker"].Value);
            if (json["cursor_tracker"] != null) cursorTracker = PlatformConfig.current.trackers.Find(i => i.id == json["cursor_tracker"].Value);
            // TODO: check if tracker is OSC
        }

        /// <summary>
        /// Copies the values from another display data.
        /// </summary>
        /// <param name="original">The original display data.</param>
        public void Clone(DisplayData original)
        {
            if (original is HoloLensData) return;
            HoloLensData holoLensData = (HoloLensData)original;

            origin = holoLensData.origin;

            // Copy the remote
            if (holoLensData.remote != null)
            {
                remote = new Remote();
                remote.address = holoLensData.remote.address;
                remote.maxBitRate = holoLensData.remote.maxBitRate;
            }

            // Copy the displays
            cullDisplays = holoLensData.cullDisplays.ToList();
            drawDisplays = holoLensData.drawDisplays.ToList();

            // Copy the trackers
            tracker = holoLensData.tracker;
            leftHandTracker = holoLensData.leftHandTracker;
            rightHandTracker = holoLensData.rightHandTracker;
            cursorTracker = holoLensData.cursorTracker;
        }
        #endregion

        #region Enums and Other Classes

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
            public int maxBitRate = 99999;

            /// <summary>
            /// Is the holoLens connected
            /// </summary>
            public bool connected = false;
        }

        public enum OriginType
        {
            START_LOCATION,
            CHOOSE_ORIGIN,
            FIND_MARKER,
        }
        #endregion
    }
}