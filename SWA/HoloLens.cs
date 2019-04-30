using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HEVS;
using OSCsharp.Data;
using OSCsharp.Net;
using OSCsharp.Utils;
using System;
using UnityEngine.XR;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Input;

public class HoloLens
{
    #region Variables

    /// <summary>
    /// The HoloLens' singleton.
    /// </summary>
    public static HoloLens current;

    // Recognises gestures performed on the HoloLens.
    private GestureRecognizer recognizer;

    // The transmitter for sending OSC
    private UDPTransmitter _transmitter;
    
    // The receiver for receiving OSC
    private UDPReceiver _receiver;

    /// <summary>
    /// The active display for the HoloLens if it exists.
    /// </summary>
    public DisplayConfig display;

    #endregion

    public HoloLens()
    {
        foreach (DisplayConfig config in NodeConfig.current.displays)
        {
            if (SWAConfig.current.holoLensConfigs.ContainsKey(config))
            {
                display = config;
                break;
            }
        }
    }

    #region Enable and Disable

    public void Enable()
    {
        Disable();
        
        // Is the holoLens
        if (display != null)
        {
            CreateTransmitter();

            // Add gesture recogniser if needed
            HoloLensConfig holoLens = display.HoloLensData();
            if (holoLens != null && holoLens.gestures.Count > 0)
            {
                recognizer = new GestureRecognizer();

                // The types of gestures to listen for
                GestureSettings gestureMask = GestureSettings.None;

                foreach (var gesture in holoLens.gestures)
                {
                    switch (gesture.type)
                    {
                        case HoloLensConfig.GestureType.TAP:
                            gestureMask |= GestureSettings.Tap;
                            break;

                        case HoloLensConfig.GestureType.DOUBLE_TAP:
                            gestureMask |= GestureSettings.DoubleTap;
                            break;

                        case HoloLensConfig.GestureType.HOLD:
                            recognizer.HoldStarted += GestureHoldStarted;
                            recognizer.HoldCompleted += GestureHoldCompleted;
                            recognizer.HoldCanceled += GestureHoldCanceled;
                            break;
                    }
                }

                if (gestureMask == GestureSettings.Tap || gestureMask == GestureSettings.DoubleTap)
                    recognizer.Tapped += GestureTapped;

                // Start the recogniser
                recognizer.SetRecognizableGestures(gestureMask);
                recognizer.StartCapturingGestures();

                SetUpHoloLens();
            }
        }

        // Receiver if this is the master
        if (Cluster.isMaster) { CreateReceiver(); }
    }

    // Turns off the receiver/transmitter
    public void Disable()
    {
        // Stop reciever if it exists
        if (_receiver != null && _receiver.IsRunning)
            _receiver.Stop();

        // Close transmitter if it exists
        if (_transmitter != null) _transmitter.Close();

        // Remove gestures
        if (recognizer != null)
        {
            recognizer.Tapped -= GestureTapped;
            recognizer.HoldStarted -= GestureHoldStarted;
            recognizer.HoldCompleted -= GestureHoldCompleted;
            recognizer.HoldCanceled -= GestureHoldCanceled;
        }
    }

    // Set up the HoloLens' camera
    private void SetUpHoloLens()
    {
        HoloLensConfig holoLens = display.HoloLensData();

        // Get the holoLens' camera's transform
        UnityEngine.Camera camera = UnityEngine.Camera.main;

        // Create a container to adjust the difference between HoloLens' origin and HEVS' origin
        Transform container = new GameObject("HoloLensContainer").transform;

        // Make the container the camera's parent
        container.parent = camera.transform.parent;
        camera.transform.parent = container;

        // Change position and direction
        if (holoLens.origin == HoloLensConfig.OriginType.START_LOCATION)
        {
            container.position = new Vector3(-camera.transform.localPosition.x, -camera.transform.localPosition.y, -camera.transform.localPosition.z);
            container.eulerAngles = new Vector3(0f, -camera.transform.localEulerAngles.y, 0f);
        }

        container.localScale = new Vector3(holoLens.scale, holoLens.scale, holoLens.scale);

        // Other Camera setup
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.nearClipPlane = 0.85f;

        // Adjust the quality if running locally
        if (holoLens.remote == null) QualitySettings.SetQualityLevel(0);
    }

    #endregion

    #region Transmitter to Master
    // Create the transmitter object
    private void CreateTransmitter()
    {
        // Create and start the transmitter
        _transmitter = new UDPTransmitter(Cluster.masterNode.address, SWAConfig.current.holoPort);
        _transmitter.Connect();
    }

    // Send the cameras transform data
    public void SendTransformData()// TODO: NEED THIS TRIGGERED ON AN EVENT
    {
        var cameraTransform = UnityEngine.Camera.main.transform;

        // Message with address "/id/transform"
        OscMessage msg = new OscMessage("/" + display.id + "/transform");

        // Get the position
        msg.Append(cameraTransform.position.x);
        msg.Append(cameraTransform.position.y);
        msg.Append(cameraTransform.position.z);

        // Get the rotation
        msg.Append(cameraTransform.rotation.x);
        msg.Append(cameraTransform.rotation.y);
        msg.Append(cameraTransform.rotation.z);
        msg.Append(cameraTransform.rotation.w);

        // Send the transform data to master
        _transmitter.Send(msg);
    }
    #endregion

    #region Gestures

    private void GestureHoldCanceled(HoldCanceledEventArgs args)
    { SendGesture("hold_canceled"); }

    private void GestureHoldCompleted(HoldCompletedEventArgs args)
    { SendGesture("hold_completed"); }

    private void GestureHoldStarted(HoldStartedEventArgs args)
    { SendGesture("hold_started"); }

    private void GestureTapped(TappedEventArgs args)
    {
        if (args.tapCount == 1) { SendGesture("tap"); }
        else { SendGesture("double_tap"); }
    }

    private void SendGesture(string gesture)
    {
        // Message with address "/id/transform"
        OscMessage msg = new OscMessage("/" + display.id + "/gesture");

        // Set the gesture
        msg.Append(gesture);

        // Send the transform data to master
        _transmitter.Send(msg);
    }

    private static void ReceiveGesture(HoloLensConfig holoLens, string gesture)
    {
        HoloLensConfig.Gesture actualGesture = null;

        switch (gesture)
        {
            case "double_tap":
                actualGesture = holoLens.gestures.Find(i => i.type == HoloLensConfig.GestureType.DOUBLE_TAP);
                break;
            case "hold_started":
            case "hold_complete":
            case "hold_canceled":
                actualGesture = holoLens.gestures.Find(i => i.type == HoloLensConfig.GestureType.HOLD);
                break;
            default:
                actualGesture = holoLens.gestures.Find(i => i.type == HoloLensConfig.GestureType.TAP);
                break;
        }

        Debug.Log(actualGesture);

        // Hold Gesture
        if (actualGesture.type == HoloLensConfig.GestureType.HOLD)
        {
            // TODO: implement hold
            throw new NotImplementedException();

            // Simple gestures
            foreach (string mapping in actualGesture.mappings) { }
        }
        else
        {
            // TODO: implement taps
            throw new NotImplementedException();

            // Simple gestures
            foreach (string mapping in actualGesture.mappings) { }
        }
    }

    #endregion

    #region Receiver for Master
    // Create the receiver object
    private void CreateReceiver()
    {
        // Create the reciever
        _receiver = new UDPReceiver(SWAConfig.current.holoPort, false);
        _receiver.MessageReceived += messageReceived;
        _receiver.ErrorOccured += oscErrorOccured;

        // Start the reciever
        _receiver.Start();
    }

    // Handles OSC messages
    private static void messageReceived(object sender, OscMessageReceivedEventArgs oscMessageReceivedEventArgs)
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
                case "gesture":
                    ReceiveGesture(display.HoloLensData(), (string)message.Data[0]);
                    break;
            }
        }
    }

    // Handles OSC errors
    private static void oscErrorOccured(object sender, ExceptionEventArgs exceptionEventArgs)
    {
        Debug.Log("HoloLens OSC Error: " + exceptionEventArgs.ToString());
    }
    #endregion
}
