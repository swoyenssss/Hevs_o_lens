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
        private UnityEngine.Camera[] _cameras;

        // Start is called before the first frame update
        void Start()
        {
            // Get the display and the camera
            _display = PlatformConfig.current.displays.Find(i => i.id == displayID);

            // Disable if needed
            if (_display == null && disableIfNotFound) gameObject.SetActive(false);
            
            // Get all the cameras for the display
            _cameras = _display.Transform().GetComponentsInChildren<UnityEngine.Camera>();

            Update();
        }

        // Update is called once per frame
        void Update()
        {
            switch (_display.displayData.type)
            {
                case DisplayType.Standard:
                    if (cullInfront)
                        foreach (UnityEngine.Camera camera in _cameras)
                            camera.nearClipPlane = Vector3.Distance(camera.transform.position, _display.transform.translate);
                    break;

                case DisplayType.OffAxis:
                    if (cullInfront)
                    {
                        OffAxisDisplayData offAxis = (OffAxisDisplayData)_display.displayData;

                        Vector3 position = _display.transform.rotate * Vector3.Scale(_display.transform.scale, offAxis.center) + _display.transform.translate;

                        foreach (UnityEngine.Camera camera in _cameras)
                            camera.nearClipPlane = Vector3.Distance(camera.transform.position, position);
                    }
                    break;

                case DisplayType.Dome:
                    throw new System.NotImplementedException();

                case DisplayType.Curved:
                    throw new System.NotImplementedException();

                    /*if (cullInfront)
                    {
                        CurvedDisplayData curved = (CurvedDisplayData)_display.displayData;

                        //Vector3 position = _display.transform.rotate * Vector3.Scale(_display.transform.scale, curved.center) + _display.transform.translate;

                        foreach (UnityEngine.Camera camera in _cameras) {

                            // Line in the direction of the camera, where does that hit the curved display

                            //curved.height * 0.5f;
                            
                            //camera.nearClipPlane = Vector3.Distance(camera.transform.position, position);
                        }
                    }
                    break;*/

                case DisplayType.AVIE:
                    throw new System.NotImplementedException();
            }
        }
    }
}