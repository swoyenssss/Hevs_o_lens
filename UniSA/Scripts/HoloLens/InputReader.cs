using System.Collections.Generic;
using UnityEngine.Windows.Speech;
#if UNITY_WSA
using UnityEngine.XR.WSA.Input;
#endif

namespace HEVS.UniSA.HoloLens
{

    /// <summary>
    /// Used to transmit input data for a tracker.
    /// </summary>
    internal class InputReader
    {
        // Stores all the input
        private Dictionary<InputType, object> _input;

#if UNITY_WSA
        // Recognises gestures performed on the HoloLens.
        private GestureRecognizer _gestureRecognizer;
#endif
        // Recognises speech performed on the HoloLens.
        private DictationRecognizer _speechRecognizer;

        // The handedness of the tracker.
        private Handedness _handedness;

        /// <summary>
        /// Constructs a HoloLens' Input object.
        /// </summary>
        /// <param name="holoLens">The device to get input for.</param>
        public InputReader(TrackerConfig tracker, Handedness handedness = Handedness.NONE)
        {
            _input = new Dictionary<InputType, object>();
            _handedness = handedness;

#if UNITY_WSA
            _gestureRecognizer = new UnityEngine.XR.WSA.Input.GestureRecognizer();
            UnityEngine.XR.WSA.Input.GestureSettings gestureMask = GetGestureMask(tracker);

            // Using gestures
            if (!gestureMask.HasFlag(UnityEngine.XR.WSA.Input.GestureSettings.None))
            {
                // Add tap to recognisor
                if (gestureMask.HasFlag(UnityEngine.XR.WSA.Input.GestureSettings.Tap) || gestureMask.HasFlag(UnityEngine.XR.WSA.Input.GestureSettings.DoubleTap))
                    _gestureRecognizer.Tapped += GestureTapped;
                
                // Add hold to recognisor
                if (gestureMask.HasFlag(UnityEngine.XR.WSA.Input.GestureSettings.Hold))
                {
                    _gestureRecognizer.HoldStarted += GestureHoldStarted;
                    _gestureRecognizer.HoldCompleted += GestureHoldCompleted;
                    _gestureRecognizer.HoldCanceled += GestureHoldCanceled;
                }

                // Add manipulation to recognisor
                if (gestureMask.HasFlag(UnityEngine.XR.WSA.Input.GestureSettings.ManipulationTranslate))
                {
                    _gestureRecognizer.ManipulationStarted += GestureManipulationStarted;
                    _gestureRecognizer.ManipulationCompleted += GestureManipulationCompleted;
                    _gestureRecognizer.ManipulationCanceled += GestureManipulationCanceled;
                    _gestureRecognizer.ManipulationUpdated += GestureManipulationUpdated; ;
                }

                // Add navigation to recognisor
                if (gestureMask.HasFlag(UnityEngine.XR.WSA.Input.GestureSettings.NavigationX) || gestureMask.HasFlag(UnityEngine.XR.WSA.Input.GestureSettings.NavigationY)
                    || gestureMask.HasFlag(UnityEngine.XR.WSA.Input.GestureSettings.NavigationZ))
                {
                    _gestureRecognizer.NavigationStarted += GestureNavigationStarted;
                    _gestureRecognizer.NavigationCompleted += GestureNavigationCompleted;
                    _gestureRecognizer.NavigationCanceled += GestureNavigationCanceled;
                    _gestureRecognizer.NavigationUpdated += GestureNavigationUpdated; ;
                }

                _gestureRecognizer.SetRecognizableGestures(gestureMask);
                _gestureRecognizer.StartCapturingGestures();
            }
#endif

            // Using speech recognition
            _speechRecognizer = new DictationRecognizer();
            _speechRecognizer.DictationResult += SpeechRecognition;
        }

        /// <summary>
        /// Returns the current input for a type.
        /// </summary>
        /// <param name="inputType">The input to get.</param>
        /// <returns>The value of the input or null.</returns>
        public object GetInput(InputType inputType)
        {
            // Use input mask as is
            if (_input.ContainsKey(inputType)) return _input[inputType];

            // otherwise not found
            return null;
        }

        /// <summary>
        /// Returns and removes current input.
        /// </summary>
        /// <param name="inputType">The input to get.</param>
        /// <returns>The value of the input null.</returns>
        public object PopInput(InputType inputType)
        {
            object value = GetInput(inputType);

            if (value == null) { return null; }

            _input.Remove(inputType);
            return value;
        }

#if UNITY_WSA

#region Taps and Hold

        private void GestureTapped(TappedEventArgs args)
        {
            if (!CorrectHand(args.source)) return;

            if (args.tapCount == 1) { _input[InputType.TAP] = true; }
            else { _input[InputType.DOUBLE_TAP] = true; }
        }

        private void GestureHoldStarted(HoldStartedEventArgs args)
        { if (CorrectHand(args.source)) _input[InputType.HOLD] = true; }

        private void GestureHoldCompleted(HoldCompletedEventArgs args)
        { if (CorrectHand(args.source)) _input[InputType.HOLD] = false; }
        private void GestureHoldCanceled(HoldCanceledEventArgs args)
        { if (CorrectHand(args.source)) _input[InputType.HOLD] = false; }
#endregion

#region Manipulation

        // Manipulation Started
        private void GestureManipulationStarted(ManipulationStartedEventArgs args)
        {
            if (CorrectHand(args.source))
            {
                _input[InputType.MANIPULATION] = true;
                SetManipulation(0f, 0f, 0f);
            }
        }

        // Manipulation Updated
        private void GestureManipulationUpdated(ManipulationUpdatedEventArgs args)
        { if (CorrectHand(args.source)) SetManipulation(args.cumulativeDelta.x, args.cumulativeDelta.y, args.cumulativeDelta.z); }

        // Manipulation Ended
        private void GestureManipulationCanceled(ManipulationCanceledEventArgs args)
        {
            if (CorrectHand(args.source))
            {
                _input[InputType.MANIPULATION] = false;
                SetManipulation(0f, 0f, 0f);
            }
        }
        private void GestureManipulationCompleted(ManipulationCompletedEventArgs args)
        {
            if (CorrectHand(args.source))
            {
                _input[InputType.MANIPULATION] = false;
                SetManipulation(0f, 0f, 0f);
            }
        }
#endregion

#region Navigation

        // Navigation Started
        private void GestureNavigationStarted(NavigationStartedEventArgs args)
        {
            if (CorrectHand(args.source))
            {
                _input[InputType.NAVIGATION] = true;
                SetNavigation(0f, 0f, 0f);
            }
        }

        // Navigation Updated
        private void GestureNavigationUpdated(NavigationUpdatedEventArgs args)
        { if (CorrectHand(args.source)) SetNavigation(args.normalizedOffset.x, args.normalizedOffset.y, args.normalizedOffset.z); }

        // Navigation Ended
        private void GestureNavigationCanceled(NavigationCanceledEventArgs args)
        {
            if (CorrectHand(args.source))
            {
                _input[InputType.NAVIGATION] = false;
                SetNavigation(0f, 0f, 0f);
            }
        }
        private void GestureNavigationCompleted(NavigationCompletedEventArgs args)
        {
            if (CorrectHand(args.source))
            {
                _input[InputType.NAVIGATION] = false;
                SetNavigation(0f, 0f, 0f);
            }
        }
#endregion
        
#region Helpers

        private void SetManipulation(float x, float y, float z)
        {
            _input[InputType.MANIPULATION_X] = x;
            _input[InputType.MANIPULATION_Y] = y;
            _input[InputType.MANIPULATION_Z] = z;
        }

        private void SetNavigation(float x, float y, float z)
        {
            _input[InputType.NAVIGATION_X] = x;
            _input[InputType.NAVIGATION_Y] = y;
            _input[InputType.NAVIGATION_Z] = z;
        }

        // Get the handedness of a source
        private bool CorrectHand(InteractionSource source)
        {
            if (_handedness == Handedness.NONE) return true;

            return (source.handedness == InteractionSourceHandedness.Right)
                == (Handedness.RIGHT_HAND == _handedness);
        }
        
        // Get the gesture mask for a tracker
        private GestureSettings GetGestureMask(TrackerConfig tracker)
        {
            GestureSettings gestureMask = GestureSettings.None;
            
            // Check for gestures
            foreach (var axis in tracker.Json()["buttons"].Children)
            {
                switch (axis["id"])
                {
                    case "tap":
                        gestureMask |= GestureSettings.Tap;
                        break;

                    case "double_tap":
                        gestureMask |= GestureSettings.DoubleTap;
                        break;

                    case "hold":
                        gestureMask |= GestureSettings.Hold;
                        break;

                    case "manipulation/x":
                    case "manipulation/y":
                    case "manipulation/z":
                        gestureMask |= GestureSettings.ManipulationTranslate;
                        break;

                    case "navigation/x":
                        gestureMask |= GestureSettings.NavigationX;
                        break;
                }
            }

            return gestureMask;
        }
#endregion
#endif

        private void SpeechRecognition(string text, ConfidenceLevel confidence) {
            if (confidence != ConfidenceLevel.Rejected)
                _input[InputType.SPEECH] = text;
        }
    }
}