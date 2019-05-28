using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEVS.UniSA
{
    
    /// <summary>
    /// Applies effects to a displays camera.
    /// </summary>
    public class DynamicCamera : MonoBehaviour
    {
        /// <summary>
        /// The id of the display to update.
        /// </summary>
        public string displayID;

        /// <summary>
        /// Disable this script if displayID is not found.
        /// </summary>
        public bool disableIfNotFound = true;

        /// <summary>
        /// If objects infront of the camera should be visible.
        /// </summary>
        public bool cullInfront = false;

        // The actual display
        private DisplayConfig _display;

        // The actual camera
        private UnityEngine.Camera _camera;

        // Start is called before the first frame update
        void Start()
        {
            // Get the display and the camera
            _display = PlatformConfig.current.displays.Find(i => i.id == displayID);

            // Disable if needed
            if (_display != null) _camera = _display.Camera();
            else if (disableIfNotFound) gameObject.SetActive(false);

            Update();
        }

        // Update is called once per frame
        void Update()
        {
            if (_camera != null)
            {
                // TODO: probably only works for off axis
                if (cullInfront) _camera.nearClipPlane = Vector3.Distance(_camera.transform.position, _display.transform.translate);
            }
        }
    }
}