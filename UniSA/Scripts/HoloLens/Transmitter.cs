using UnityEngine;
using OSCsharp.Data;
using OSCsharp.Net;
using System.Collections.Generic;

namespace HEVS.UniSA.HoloLens
{

    /// <summary>
    /// Sends HoloLens tracker info using OSC.
    /// </summary>
    internal class Transmitter
    {
        // Store all transmitters
        private static Dictionary<int,Transmitter> _transmitters;
        
        /// <summary>
        /// Get a transmitter for a port.
        /// </summary>
        public static Transmitter GetTransmitter(int port) {
            if (_transmitters == null) { _transmitters = new Dictionary<int, Transmitter>(); }
            else if (_transmitters.ContainsKey(port)) return _transmitters[port];

            Transmitter transmitter = new Transmitter(port);
            _transmitters[port] = transmitter;
            return transmitter;
        }

        /// <summary>
        /// Close all the transmitters
        /// </summary>
        public static void CloseAll() {
            if (_transmitters == null) return;

            foreach (Transmitter transmitter in _transmitters.Values) {
                if (transmitter._transmitterX != null) transmitter._transmitter.Close();
                if (transmitter._transmitter != null) transmitter._transmitter.Close();
            }
        }

        // The transmitter for sending OSC
        private UDPTransmitter _transmitter;
        // The transmitter for sending OSC
        private UDPTransmitter _transmitterX;

        // Construct a transmitter to send hololens input.
        private Transmitter(int port)
        {
            // Create and start the transmitter
            _transmitter = new UDPTransmitter(Cluster.masterNode.address, port);// TODO: only have one transmitter
            _transmitterX = new UDPTransmitter(Cluster.masterNode.address, HoloLensConfig.current.holoPort);
            _transmitter.Connect();
        }

        /// <summary>
        /// Sends the holoLen's input and transform to the master as the tracker config.
        /// </summary>
        /// <param name="tracker">The tracker config.</param>
        /// <param name="input">The input for the tracker.</param>
        /// <param name="transform">The transform for the tracker.</param>
        public static void SendTracker(TrackerConfig tracker, InputReader input, TransformConfig transform)
        {
            Transmitter transmitter = GetTransmitter(((OSCTrackerData)tracker.data).port);

            // Send the transform data
            Vector3 rotate = transform.rotate.eulerAngles;
            transmitter.SendOSC(tracker.id + "/transform",
                transform.translate.x, transform.translate.y, transform.translate.z, rotate.x, rotate.y, rotate.z);
            
            // Send each of the inputs
            foreach (var axis in tracker.Json()["buttons"].Children)
            {
                InputType inputType = StringToInputType(axis["id"]);


                // Ignore bad axises
                switch (inputType)
                {
                    case InputType.NONE:
                        // Dilberately empty
                        break;

                    case InputType.SPEECH:
                        if (axis["id"] == "speech" + input.PopInput(InputType.SPEECH))
                            transmitter.SendOSCX(tracker.id + "/button/" + axis["id"], true);
                        break;

                    default:
                        object value = input.PopInput(inputType);
                        if (value != null) transmitter.SendOSCX(tracker.id + "/button/" + axis["id"], value);
                        break;
                }
            }
        }

        /// <summary>
        /// Converts a string into an InputType.
        /// </summary>
        /// <param name="axis">The axis as a string.</param>
        /// <returns>The axis as an InputType</returns>
        private static InputType StringToInputType(string axis)
        {
            switch (axis)
            {
                case "tap":
                    return InputType.TAP;
                case "double_tap":
                    return InputType.DOUBLE_TAP;
                case "hold":
                    return InputType.HOLD;
                case "manipulation":
                    return InputType.MANIPULATION_X;
                case "manipulation_x":
                    return InputType.MANIPULATION_X;
                case "manipulation_y":
                    return InputType.MANIPULATION_Y;
                case "manipulation_z":
                    return InputType.MANIPULATION_Z;
                case "navigation":
                    return InputType.NAVIGATION_X;
                case "navigation_x":
                    return InputType.NAVIGATION_X;
                case "navigation_y":
                    return InputType.NAVIGATION_Y;
                case "navigation_z":
                    return InputType.NAVIGATION_Z;
            }

            if (axis.StartsWith("speech:")) return InputType.SPEECH;

            return InputType.NONE;
        }

        // Send data through OSC
        private void SendOSC(string address, params object[] args)
        {
            // Message with address "/id/button_gesture"
            OscMessage msg = new OscMessage(address);

            // Set the gesture
            foreach (object arg in args)
                msg.Append(arg);

            // Send the transform data to master
            _transmitter.Send(msg);
        }

        // Send data through OSC
        private void SendOSCX(string address, params object[] args)
        {
            // Message with address "/id/button_gesture"
            OscMessage msg = new OscMessage(address);

            // Set the gesture
            foreach (object arg in args)
                msg.Append(arg);

            // Send the transform data to master
            _transmitterX.Send(msg);
        }
    }
}