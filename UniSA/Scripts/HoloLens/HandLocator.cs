#if UNITY_WSA
using UnityEngine;
using UnityEngine.XR.WSA.Input;
#endif

namespace HEVS.UniSA.HoloLens
{

    /// <summary>
    /// Used to track the position of a hand from HoloLens.
    /// </summary>
    internal class HandLocator
    {

        private TransformConfig _transform;
        /// <summary>
        /// The current transform of the hand.
        /// </summary>
        public TransformConfig transform { get { return _transform; } }

        // Which hand this tracks
        private Handedness _handedness;

        /// <summary>
        /// Constructs a hand tracker object.
        /// </summary>
        /// <param name="handedness">Which hand to use.</param>
        public HandLocator(Handedness handedness)
        {
            _handedness = handedness;
#if UNITY_WSA
            InteractionManager.InteractionSourceUpdated += InteractionSourceUpdated;
#endif
        }

#if UNITY_WSA
        // Sends the hands transform when it updates.
        private void InteractionSourceUpdated(InteractionSourceUpdatedEventArgs args)
        {
            if (!CorrectHand(args.state.source)) return;

            Vector3 position = new Vector3();
            Quaternion rotation = new Quaternion();

            // Try getting the position and rotation
            args.state.sourcePose.TryGetPosition(out position);
            args.state.sourcePose.TryGetRotation(out rotation);

            // Send all the transform data
            _transform.translate = position;
            _transform.rotate = rotation;
        }

        // Get the handedness of a source
        private bool CorrectHand(UnityEngine.XR.WSA.Input.InteractionSource source)
        {
            if (_handedness == Handedness.NONE) return true;

            return (source.handedness == InteractionSourceHandedness.Right)
                == (Handedness.RIGHT_HAND == _handedness);
        }
#endif
    }
}