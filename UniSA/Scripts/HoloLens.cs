using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HEVS;
using OSCsharp.Data;
using OSCsharp.Net;
using OSCsharp.Utils;
using System;
#if UNITY_WSA
using UnityEngine.XR;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Input;
#endif

namespace HEVS.UniSA
{

    /// <summary>
    /// Exists only on master node to take holoLens input form HoloLensDevices.
    /// </summary>
    public class HoloLens
    {
        #region Variables

        /// <summary>
        /// The HoloLens' singleton.
        /// </summary>
        public static HoloLens current;

        // The receiver for receiving OSC
        private UDPReceiver _receiver;
        #endregion

        // Create the receiver object
        public HoloLens()
        {
            // Create the reciever
            _receiver = new UDPReceiver(UniSAConfig.current.holoPort, false);
            _receiver.MessageReceived += MessageReceived;
            _receiver.ErrorOccured += OscErrorOccured;

            // Start the reciever
            _receiver.Start();

            // Adds handler for HoloLens input
            Input.AccumulateCustomInput += HoloLensInput;
        }

        /// <summary>
        /// Turns off the receiver
        /// </summary>
        public void Disable()
        {
            // Stop reciever if it exists
            if (_receiver != null && _receiver.IsRunning)
                _receiver.Stop();
        }

        // Handles OSC messages
        private static void MessageReceived(object sender, OscMessageReceivedEventArgs oscMessageReceivedEventArgs)
        {
            OscMessage message = oscMessageReceivedEventArgs.Message;

            // Read the address part by part
            string[] address = message.Address.TrimStart('/').Split('/');

            // Get the display via the address
            DisplayConfig display = PlatformConfig.current.displays.Find(i => i.id == address[0]);

            // For the type of the message
            if (display != null)
            {
                switch (address[1])
                {
                    case "transform":
                        // Store the transform
                        display.transform.translate = new Vector3((float)message.Data[0], (float)message.Data[1], (float)message.Data[2]);
                        display.transform.rotate = new Quaternion((float)message.Data[3], (float)message.Data[4], (float)message.Data[5], (float)message.Data[6]);
                        break;

                    case "button_gesture":
                        ReceiveButtonGesture(display.HoloLensData(), (string)message.Data[0]);
                        break;

                    case "axes_gesture":
                        ReceiveAxesGesture(display.HoloLensData(), (string)message.Data[0],
                            new Vector3((float)message.Data[1], (float)message.Data[2], (float)message.Data[3]));
                        break;
                }
            }
        }

        // Handles OSC errors
        private static void OscErrorOccured(object sender, ExceptionEventArgs exceptionEventArgs)
        {
            Debug.LogError("HoloLens OSC Error: " + exceptionEventArgs.ToString());
        }

        // 
        private static void ReceiveButtonGesture(HoloLensData holoLens, string gesture)
        {
            switch (gesture)
            {
                case "tap":
                    holoLens.buttonGestures.Find(i => i.type == HoloLensData.ButtonGesture.Type.TAP).performed = true;
                    break;
                case "double_tap":
                    holoLens.buttonGestures.Find(i => i.type == HoloLensData.ButtonGesture.Type.DOUBLE_TAP).performed = true;
                    break;
                case "hold_started":
                    holoLens.buttonGestures.Find(i => i.type == HoloLensData.ButtonGesture.Type.HOLD).performed = true;
                    break;
                case "hold_ended":
                    holoLens.buttonGestures.Find(i => i.type == HoloLensData.ButtonGesture.Type.HOLD).performed = false;
                    break;
            }
        }

        //
        private static void ReceiveAxesGesture(HoloLensData holoLens, string gesture, Vector3 axes)
        {
            switch (gesture)
            {
                case "manipulation":
                    holoLens.axesGestures.Find(i => i.type == HoloLensData.AxesGesture.Type.MANIPULATION).axes = axes;
                    break;
                case "navigation":
                    holoLens.axesGestures.Find(i => i.type == HoloLensData.AxesGesture.Type.NAVIGATION).axes = axes;
                    break;
            }
        }

        //
        private void HoloLensInput()
        {
            foreach (HoloLensData holoLens in UniSAConfig.current.holoLensConfigs.Values)
            {
                // Gesture button input
                foreach (var gesture in holoLens.buttonGestures)
                {
                    if (gesture.performed)
                    {
                        if (gesture.type != HoloLensData.ButtonGesture.Type.HOLD)
                            gesture.performed = false;

                        foreach (string mapping in gesture.mappings)
                            Input.ForceButtonThisFrame(mapping);
                    }
                }

                // Gesture axes input
                foreach (var gesture in holoLens.axesGestures)
                {

                    // Set x axis
                    foreach (string mappingX in gesture.mappingsX)
                        Input.AccumulateAxisThisFrame(mappingX, gesture.axes.x);

                    // Set y axis
                    foreach (string mappingY in gesture.mappingsY)
                        Input.AccumulateAxisThisFrame(mappingY, gesture.axes.y);

                    // Set z axis
                    foreach (string mappingZ in gesture.mappingsZ)
                        Input.AccumulateAxisThisFrame(mappingZ, gesture.axes.z);
                }
            }
        }
    }
}