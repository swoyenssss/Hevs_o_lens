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
    /// Exists only on HoloLens devices to send transform and input.
    /// </summary>
    public class HoloLensDevice
    {
        #region Variables

        /// <summary>
        /// The HoloLensDevice's singleton.
        /// </summary>
        public static HoloLensDevice current;

        /// <summary>
        /// The active display for the HoloLens if it exists.
        /// </summary>
        public DisplayConfig display;

#if UNITY_WSA
        // Recognises gestures performed on the HoloLens.
        private GestureRecognizer _recognizer;

        // The transmitter for sending OSC
        private UDPTransmitter _transmitter;
#endif
        #endregion

        public HoloLensDevice(DisplayConfig display)
        {
            this.display = display;

#if UNITY_WSA
            var remote = display.HoloLensData().remote;

            // Create and start the transmitter
            _transmitter = new UDPTransmitter(Cluster.masterNode.address, UniSAConfig.current.holoPort);
            _transmitter.Connect();

            // Wait for remoting
            if (remote != null) HolographicRemoting.Connect(remote.address, remote.maxBitRate);
            else
#endif
            { Enable(); }
        }

        #region Start Up

        private void Enable()
        {

#if UNITY_WSA
            // Add gesture recogniser if needed
            HoloLensData holoLens = display.HoloLensData();

            _recognizer = new GestureRecognizer();

            // The types of gestures to listen for
            GestureSettings gestureMask = GestureSettings.None;

            // Check for gesture buttons
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
                        _recognizer.HoldStarted += GestureHoldStarted;
                        _recognizer.HoldCompleted += GestureHoldCompleted;
                        _recognizer.HoldCanceled += GestureHoldCanceled;
                        break;
                }
            }

            // Use tapped if either tap or double tap were found
            if (gestureMask.HasFlag(GestureSettings.Tap) || gestureMask.HasFlag(GestureSettings.DoubleTap))
                _recognizer.Tapped += GestureTapped;

            // Check for gesture axes
            foreach (var gesture in holoLens.axesGestures)
            {
                switch (gesture.type)
                {
                    case HoloLensData.AxesGesture.Type.MANIPULATION:
                        gestureMask |= GestureSettings.ManipulationTranslate;
                        _recognizer.ManipulationStarted += GestureManipulationStarted;
                        _recognizer.ManipulationCompleted += GestureManipulationCompleted;
                        _recognizer.ManipulationCanceled += GestureManipulationCanceled;
                        _recognizer.ManipulationUpdated += GestureManipulationUpdated; ;
                        break;

                    case HoloLensData.AxesGesture.Type.NAVIGATION:
                        if (gesture.mappingsX.Count != 0) { gestureMask |= GestureSettings.NavigationX; }
                        if (gesture.mappingsX.Count != 0) { gestureMask |= GestureSettings.NavigationY; }
                        if (gesture.mappingsX.Count != 0) { gestureMask |= GestureSettings.NavigationZ; }
                        _recognizer.NavigationStarted += GestureNavigationStarted;
                        _recognizer.NavigationCompleted += GestureNavigationCompleted;
                        _recognizer.NavigationCanceled += GestureNavigationCanceled;
                        _recognizer.NavigationUpdated += GestureNavigationUpdated; ;
                        break;
                }

                // Start the recogniser
                _recognizer.SetRecognizableGestures(gestureMask);
                _recognizer.StartCapturingGestures();
            }
#endif
            SetUpHoloLens();
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
                container.position = -camera.transform.position;
                container.eulerAngles = new Vector3(0f, -camera.transform.localEulerAngles.y, 0f);

#if UNITY_WSA
                // Create world anchor
                WorldAnchor worldAnchor = new GameObject("WorldAnchor").AddComponent<WorldAnchor>();
                worldAnchor.transform.position = camera.transform.position;
                worldAnchor.transform.rotation = camera.transform.rotation;
#endif
            }

            Transform cullParent = new GameObject("Trackers").transform;
            cullParent.hideFlags = HideFlags.HideInHierarchy;
            cullParent.parent = container.parent;
            cullParent.localPosition = display.transform.translate;
            cullParent.localRotation = display.transform.rotate;
            cullParent.localScale = display.transform.scale;

            // Cull Displays
            foreach (DisplayConfig display in holoLens.cullDisplays)
            {
                GameObject gameObject = new GameObject(display.id + "-CullDisplay");
                gameObject.hideFlags = HideFlags.HideInHierarchy;
                gameObject.transform.parent = cullParent;
                RenderDisplay(display, gameObject, Resources.Load<Material>("CullHoloLens"));
            }
            
            // Draw Displays
            foreach (DisplayConfig display in holoLens.drawDisplays)
            {
                GameObject gameObject = new GameObject(display.id + "-Display");
                gameObject.hideFlags = HideFlags.HideInHierarchy;
                gameObject.transform.parent = container.parent;
                RenderDisplay(display, gameObject, Resources.Load<Material>("DrawDisplay"));
            }
            
            // Update container transform
            container.localPosition += display.transform.translate;
            container.localRotation *= display.transform.rotate;
            container.localScale = display.transform.scale;

            // Other Camera setup
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.nearClipPlane = 0.85f;

            // Adjust the quality if running locally
            if (holoLens.remote == null) QualitySettings.SetQualityLevel(0);
        }
        
        private void RenderDisplay(DisplayConfig display, GameObject gameObject, Material material)
        {
            DisplayTransform displayTracker = gameObject.AddComponent<DisplayTransform>();
            displayTracker.displayID = display.id;

            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = material;

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = display.CreateMesh();
        }
        #endregion

        #region While Running

        /// <summary>
        /// Called by UniSAConfig to set up remoting or send transform data.
        /// </summary>
        public void Update()
        {
            var remote = display.HoloLensData().remote;

#if UNITY_WSA
            // If remoting has now connected
            if (remote != null && remote.connected == false
                && HolographicRemoting.ConnectionState == HolographicStreamerConnectionState.Connected)
            {
                remote.connected = true;
                UniSAConfig.current.StartCoroutine(LoadDevice("WindowsMR"));
                Enable();
            }
#endif
            // Send transform data.
            if (remote == null || remote.connected == true) SendTransformData();
        }

        // Neccessary for remoting.
        IEnumerator LoadDevice(string newDevice)
        {
            XRSettings.LoadDeviceByName(newDevice);
            yield return null;
            XRSettings.enabled = true;
        }

        /// <summary>
        /// Turns off the transmitter
        /// </summary>
        public void Disable()
        {
#if UNITY_WSA
            // Close transmitter if it exists
            if (_transmitter != null) _transmitter.Close();

            // Remove gestures
            if (_recognizer != null)
            {
                _recognizer.Tapped -= GestureTapped;
                _recognizer.HoldStarted -= GestureHoldStarted;
                _recognizer.HoldCompleted -= GestureHoldCompleted;
                _recognizer.HoldCanceled -= GestureHoldCanceled;
            }
#endif
        }
#endregion

        #region Send Transform Data

        // Send the cameras transform data
        public void SendTransformData()
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

#if UNITY_WSA

        #region Send Button Gestures

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
        #endregion

        #region Send Axes Gestures

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
    }
}