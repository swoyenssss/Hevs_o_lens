using UnityEngine;
using System.Linq;

namespace HEVS.UniSA
{
    
    /// <summary>
    /// Transforms to match a displays transform.
    /// </summary>
    public class DisplayTransform : ClusterTransform
    {
        /// <summary>
        /// The ID for the display's config.
        /// </summary>
        public string displayID;
        
        /// <summary>
        /// Disable this script if displayID is not found.
        /// </summary>
        public bool disableIfNotFound = true;

        // The actual display
        private DisplayConfig _display;

        // Find the display
        new void Start()
        {
            base.Start();
            
            // Get the display and the camera
            _display = PlatformConfig.current.displays.Find(i => i.id == displayID);

            // Disable if needed
            if (_display == null && disableIfNotFound) gameObject.SetActive(false);

            FixedUpdate();
        }

        // Update transform to match the display
        void FixedUpdate()
        {
            // Update transform if there is a receiver
            if (Cluster.isMaster && _display != null)
            {
                transform.localPosition = _display.transform.translate;
                transform.localRotation = _display.transform.rotate;
                transform.localScale = _display.transform.scale;
            }
        }
    }
}