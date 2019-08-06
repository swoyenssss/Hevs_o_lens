
namespace HEVS.UniSA
{

    /// <summary>
    /// Handles the input for a HoloLens.
    /// </summary>
    internal static class InputController
    {

        public static void Start() {
            MixedRealityDisplay display = MixedRealityDisplay.currentDisplay;

            // Create input handlers
            InputHandler input = null, leftHandInput = null, rightHandInput = null, cursorInput = null;
            HandLocator leftHand = null, rightHand = null;
            CursorLocator cursor = null;
            
            if (display.tracker != null)
                input = new InputHandler(display.tracker);

            if (display.leftHandTracker != null) {
                leftHand = new HandLocator(TrackerHandedness.Left);
                leftHandInput = new InputHandler(display.leftHandTracker, TrackerHandedness.Left); ;
            }

            if (display.rightHandTracker != null) {
                rightHand = new HandLocator(TrackerHandedness.Right);
                rightHandInput = new InputHandler(display.rightHandTracker, TrackerHandedness.Right); ;
            }

            if (display.cursorTracker != null) {
                cursor = new CursorLocator(Camera.main.transform);
                cursorInput = new InputHandler(display.cursorTracker); ;
            }
            
            // Transmit the transform data
            Configuration.OnPreUpdate += () => {
                if (display.tracker != null)
                    Transmitter.SendTransform(display.tracker, new TransformConfig().Concatenate(Camera.main.transform));
                
                if (display.leftHandTracker != null)
                    Transmitter.SendTransform(display.leftHandTracker, leftHand.transform);

                if (display.rightHandTracker != null)
                    Transmitter.SendTransform(display.rightHandTracker, rightHand.transform);

                if (display.cursorTracker != null)
                    Transmitter.SendTransform(display.cursorTracker, cursor.transform);
            };
        }
    }
}