using UnityEngine;

namespace HEVS.UniSA
{

    /// <summary>
    /// Updates the display to match this object's transform.
    /// </summary>
    public class DynamicDisplay : MonoBehaviour
    {

        /// <summary>
        /// The id of the display to update.
        /// </summary>
        public string displayID;

        /// <summary>
        /// Disable this script if displayID is not found.
        /// </summary>
        public bool disableIfNotFound = true;

        // The actual display
        private DisplayConfig _display;
        
        // Find the display
        void Start()
        {
            // Get the display and the camera
            _display = PlatformConfig.current.displays.Find(i => i.id == displayID);

            // Disable if needed
            if (_display == null && disableIfNotFound) gameObject.SetActive(false);

            Update();
        }
        
        // Update the display's transform
        void Update()
        {
            if (_display != null)
            {
                _display.transform.translate = transform.position - SceneOrigin.position;
                _display.transform.rotate = transform.rotation;
                _display.transform.scale = transform.lossyScale;
            }

            // TODO: Curve display cameras do no get actively updated
        }
    }
}