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


public class HoloLens
{
#region Variables

    /// <summary>
    /// The HoloLens' singleton.
    /// </summary>
    public static HoloLens current;

#if UNITY_WSA
    // Recognises gestures performed on the HoloLens.
    private GestureRecognizer recognizer;
#endif

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

#if UNITY_WSA
        // Is the holoLens
        if (display != null)
        {
            CreateTransmitter();

            // Adds handler for HoloLens input
            HEVS.Input.AccumulateCustomInput += HoloLensInput;

            // Add gesture recogniser if needed
            HoloLensData holoLens = display.HoloLensData();
            if (holoLens != null && holoLens.axesGestures.Count > 0)
            {
                recognizer = new GestureRecognizer();

                // The types of gestures to listen for
                GestureSettings gestureMask = GestureSettings.None;

                foreach (var gesture in holoLens.buttonGestures)
                {
                    switch (gesture.type)
                    {
                        case HoloLensData.ButtonGesture.Type.TAP:
                            gestureMask |= GestureSettings.Tap;
                            break;

                        case HoloLensData.ButtonGesture.Type.DOUBLE_TAP:
                            gestureMask |= GestureSettings.DoubleTap;
                            break;

                        case HoloLensData.ButtonGesture.Type.HOLD:
                            gestureMask |= GestureSettings.Hold;
                            recognizer.HoldStarted += GestureHoldStarted;
                            recognizer.HoldCompleted += GestureHoldCompleted;
                            recognizer.HoldCanceled += GestureHoldCanceled;
                            break;
                    }
                }

                if (gestureMask.HasFlag(GestureSettings.Tap) || gestureMask.HasFlag(GestureSettings.DoubleTap))
                    recognizer.Tapped += GestureTapped;

                foreach (var gesture in holoLens.axesGestures)
                {
                    switch (gesture.type)
                    {
                        case HoloLensData.AxesGesture.Type.MANIPULATION:
                            gestureMask |= GestureSettings.ManipulationTranslate;
                            recognizer.ManipulationStarted += GestureManipulationStarted;
                            recognizer.ManipulationCompleted += GestureManipulationCompleted;
                            recognizer.ManipulationCanceled += GestureManipulationCanceled;
                            recognizer.ManipulationUpdated += GestureManipulationUpdated; ;
                            break;

                        case HoloLensData.AxesGesture.Type.NAVIGATION:
                            if (gesture.mappingsX.Count != 0) { gestureMask |= GestureSettings.NavigationX; }
                            if (gesture.mappingsX.Count != 0) { gestureMask |= GestureSettings.NavigationY; }
                            if (gesture.mappingsX.Count != 0) { gestureMask |= GestureSettings.NavigationZ; }
                            recognizer.NavigationStarted += GestureNavigationStarted;
                            recognizer.NavigationCompleted += GestureNavigationCompleted;
                            recognizer.NavigationCanceled += GestureNavigationCanceled;
                            recognizer.NavigationUpdated += GestureNavigationUpdated; ;
                            break;
                    }
                }


                // Start the recogniser
                recognizer.SetRecognizableGestures(gestureMask);
                recognizer.StartCapturingGestures();
            }

            SetUpHoloLens();
        }
#endif

        // Receiver if this is the master
        if (Cluster.isMaster) { CreateReceiver(); }
    }

    // Turns off the receiver/transmitter
    public void Disable()
    {
        // Stop reciever if it exists
        if (_receiver != null && _receiver.IsRunning)
            _receiver.Stop();

#if UNITY_WSA
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
#endif
    }

    // Set up the HoloLens' camera
    private void SetUpHoloLens()
    {
        HoloLensData holoLens = display.HoloLensData();

        // Get the holoLens' camera's transform
        UnityEngine.Camera camera = UnityEngine.Camera.main;

        // Create a container to adjust the difference between HoloLens' origin and HEVS' origin
        Transform container = new GameObject("HoloLensContainer").transform;

        // Make the container the camera's parent
        container.parent = camera.transform.parent;
        camera.transform.parent = container;

        // Change position and direction
        if (holoLens.origin == HoloLensData.OriginType.START_LOCATION)
        {
            container.position = -camera.transform.position * holoLens.scale;
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

#if UNITY_WSA
    #region Transmitter to Master
    // Create the transmitter object
    private void CreateTransmitter()
    {
        // Create and start the transmitter
        _transmitter = new UDPTransmitter("10.160.99.31"/*Cluster.masterNode.address*/, SWAConfig.current.holoPort);
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

    private void GestureHoldStarted(HoldStartedEventArgs args)
    { SendButtonGesture("hold_started"); }

    private void GestureHoldCompleted(HoldCompletedEventArgs args)
    { SendButtonGesture("hold_ended"); }
    private void GestureHoldCanceled(HoldCanceledEventArgs args)
    { SendButtonGesture("hold_ended"); }
    
    private void GestureTapped(TappedEventArgs args)
    {
        if (args.tapCount == 1) { SendButtonGesture("tap"); }
        else { SendButtonGesture("double_tap"); }
    }

    private void SendButtonGesture(string gesture)
    {
        // Message with address "/id/button_gesture"
        OscMessage msg = new OscMessage("/" + display.id + "/button_gesture");

        // Set the gesture
        msg.Append(gesture);
        
        // Send the transform data to master
        _transmitter.Send(msg);
    }

    // Manipulation Started
    private void GestureManipulationStarted(ManipulationStartedEventArgs args)
    { SendAxesGesture("manipulation", Vector3.zero); }

    private void GestureManipulationUpdated(ManipulationUpdatedEventArgs args)
    { SendAxesGesture("manipulation", args.cumulativeDelta); }

    // Manipulation Ended
    private void GestureManipulationCanceled(ManipulationCanceledEventArgs args)
    { SendAxesGesture("manipulation", Vector3.zero); }
    private void GestureManipulationCompleted(ManipulationCompletedEventArgs args)
    { SendAxesGesture("manipulation", Vector3.zero); }

    // Navigation Started
    private void GestureNavigationStarted(NavigationStartedEventArgs args)
    { SendAxesGesture("navigation", Vector3.zero); }

    private void GestureNavigationUpdated(NavigationUpdatedEventArgs args)
    { SendAxesGesture("navigation", args.normalizedOffset); }

    // Navigation Ended
    private void GestureNavigationCanceled(NavigationCanceledEventArgs args)
    { SendAxesGesture("navigation", Vector3.zero); }
    private void GestureNavigationCompleted(NavigationCompletedEventArgs args)
    { SendAxesGesture("navigation", Vector3.zero); }

    private void SendAxesGesture(string gesture, Vector3 axes)
    {
        // Message with address "/id/axes_gesture"
        OscMessage msg = new OscMessage("/" + display.id + "/axes_gesture");

        // Set the gesture
        msg.Append(gesture);
        msg.Append(axes.x);
        msg.Append(axes.y);
        msg.Append(axes.z);

        // Send the transform data to master
        _transmitter.Send(msg);
    }

    #endregion
#endif

    #region Receiver for Master
    private static void ReceiveButtonGesture(HoloLensData holoLens, string gesture)
    {
        HoloLensData.ButtonGesture actualGesture = null;

        switch (gesture)
        {
            case "tap":
                actualGesture = holoLens.buttonGestures.Find(i => i.type == HoloLensData.ButtonGesture.Type.TAP);
                actualGesture.performed = true;
                break;
            case "double_tap":
                actualGesture = holoLens.buttonGestures.Find(i => i.type == HoloLensData.ButtonGesture.Type.DOUBLE_TAP);
                actualGesture.performed = true;
                break;
            case "hold_started":
                actualGesture = holoLens.buttonGestures.Find(i => i.type == HoloLensData.ButtonGesture.Type.HOLD);
                actualGesture.performed = true;
                break;
            case "hold_ended":
                actualGesture = holoLens.buttonGestures.Find(i => i.type == HoloLensData.ButtonGesture.Type.HOLD);
                actualGesture.performed = false;
                break;
        }
    }

    private static void ReceiveAxesGesture(HoloLensData holoLens, string gesture, Vector3 axes)
    {
        HoloLensData.AxesGesture actualGesture = null;
        
        switch (gesture)
        {
            case "manipulation":
                actualGesture = holoLens.axesGestures.Find(i => i.type == HoloLensData.AxesGesture.Type.MANIPULATION);
                actualGesture.axes = axes;
                break;
            case "navigation":
                actualGesture = holoLens.axesGestures.Find(i => i.type == HoloLensData.AxesGesture.Type.NAVIGATION);
                actualGesture.axes = axes;
                break;
        }
    }

    // Create the receiver object
    private void CreateReceiver()
    {
        // Create the reciever
        _receiver = new UDPReceiver(SWAConfig.current.holoPort, false);
        _receiver.MessageReceived += MessageReceived;
        _receiver.ErrorOccured += OscErrorOccured;

        // Start the reciever
        _receiver.Start();
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
        Debug.Log("HoloLens OSC Error: " + exceptionEventArgs.ToString());
    }
#endregion

    private void HoloLensInput ()
    {
        foreach (HoloLensData holoLens in SWAConfig.current.holoLensConfigs.Values)
        {
            // Gesture button input
            foreach (var gesture in holoLens.buttonGestures)
            {
                if (gesture.performed)
                {
                    if (gesture.type != HoloLensData.ButtonGesture.Type.HOLD)
                        gesture.performed = false;

                    foreach (string mapping in gesture.mappings)
                        HEVS.Input.ForceButtonThisFrame(mapping);
                }
            }

            // Gesture axes input
            foreach (var gesture in holoLens.axesGestures)
            {

                // Set x axis
                foreach (string mappingX in gesture.mappingsX)
                    HEVS.Input.AccumulateAxisThisFrame(mappingX, gesture.axes.x);

                // Set y axis
                foreach (string mappingY in gesture.mappingsY)
                    HEVS.Input.AccumulateAxisThisFrame(mappingY, gesture.axes.y);

                // Set z axis
                foreach (string mappingZ in gesture.mappingsZ)
                    HEVS.Input.AccumulateAxisThisFrame(mappingZ, gesture.axes.z);
            }
        }
    }
}
