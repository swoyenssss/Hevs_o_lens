
namespace HEVS.UniSA.HoloLens
{

    /// <summary>
    /// Handles the input for a HoloLens.
    /// </summary>
    internal class InputController
    {
        /// <summary>
        /// The HoloLens this is handling input for.
        /// </summary>
        public HoloLensDevice holoLens { get; private set; }

        // Regular input
        private InputReader _input;

        // Cursor
        private CursorLocator _cursor;
        private InputReader _cursorInput;

        // Left hand
        private HandLocator _leftHand;
        private InputReader _leftHandInput;

        // Right hand
        private HandLocator _rightHand;
        private InputReader _rightHandInput;

        /// <summary>
        /// Constructs an input handler for a HoloLens.
        /// </summary>
        /// <param name="holoLens">The HoloLens to use</param>
        public InputController(HoloLensDevice holoLens)
        {
            this.holoLens = holoLens;
            HoloLensData data = holoLens.display.HoloLensData();

            if (data.tracker != null)
                _input = new InputReader(data.tracker);

            if (data.cursorTracker != null)
            {
                _cursor = new CursorLocator(holoLens.transform);
                _cursorInput = new InputReader(data.cursorTracker); ;
            }

            if (data.leftHandTracker != null)
            {
                _leftHand = new HandLocator(Handedness.LEFT_HAND);
                _leftHandInput = new InputReader(data.leftHandTracker, Handedness.LEFT_HAND); ;
            }

            if (data.rightHandTracker != null)
            {
                _rightHand = new HandLocator(Handedness.RIGHT_HAND);
                _rightHandInput = new InputReader(data.rightHandTracker, Handedness.RIGHT_HAND); ;
            }
        }

        /// <summary>
        /// Transmits all the input the master.
        /// </summary>
        public void SendInput()
        {
            HoloLensData data = holoLens.display.HoloLensData();

            if (data.tracker != null)
                Transmitter.SendTracker(data.tracker, _input, new TransformConfig().Concatenate(holoLens.transform));

            if (data.cursorTracker != null)
                Transmitter.SendTracker(data.cursorTracker, _cursorInput, _cursor.transform);

            if (data.leftHandTracker != null)
                Transmitter.SendTracker(data.leftHandTracker, _leftHandInput, _leftHand.transform);

            if (data.rightHandTracker != null)
                Transmitter.SendTracker(data.rightHandTracker, _rightHandInput, _rightHand.transform);
        }
    }
}