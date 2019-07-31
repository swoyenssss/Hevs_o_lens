using System;
using System.Collections;
using UnityEngine;
using System.IO;
#if UNITY_WSA
using UnityEngine.XR;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Persistence;
using UnityEngine.XR.WSA.Sharing;
#endif

namespace HEVS.UniSA.HoloLens {
    /// <summary>
    /// Exists only on HoloLens devices to send transform and input.
    /// </summary>
    internal class HoloLensDevice {
        #region Variables

        /// <summary>
        /// The active display for the HoloLens if it exists.
        /// </summary>
        public DisplayConfig display;

        /// <summary>
        /// The transform of the HoloLens' camera.
        /// </summary>
        public Transform transform;

        // True if the holoLens has been set up
        private bool _holoLensStarted;

        // Used to handle input
        private InputController _input;

        private OriginController _origin;

        private MemoryStream _originData;

        #endregion

        public HoloLensDevice(Transform transform, DisplayConfig display) {
            // Must have holoLens data
            if (display.HoloLensData() == null) throw new Exception("Display does not contain HoloLensData.");

            this.transform = transform;
            this.display = display;

            // Create containers
            Transform outerContainer = new GameObject(display.id + "-HoloTransform").transform;
            Transform innerContainer = new GameObject(display.id + "-HoloContainer").transform;
            outerContainer.SetParent(transform.parent, false);
            innerContainer.SetParent(outerContainer, false);
            transform.SetParent(innerContainer, false);

            // Create the origin controller
            _origin = new OriginController(this);

#if UNITY_WSA
            var remote = display.HoloLensData().remote;

            // Wait for remoting or start
            if (remote != null) HolographicRemoting.Connect(remote.address, remote.maxBitRate);
            else {
                _holoLensStarted = true;
                SetUpHoloLens();
            };
#else
            _holoLensStarted = true;
            SetUpHoloLens();
#endif
        }

        // Set up the HoloLens' camera
        private void SetUpHoloLens() {
            HoloLensData holoLens = display.HoloLensData();

            // Get the holoLens' camera's transform
            UnityEngine.Camera camera = transform.gameObject.GetComponent<UnityEngine.Camera>();

            // Other Camera setup
            if (holoLens.disableBackground) {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }

            camera.nearClipPlane = holoLens.clippingPlane;

            // Adjust the quality if running locally
            if (holoLens.remote == null) QualitySettings.SetQualityLevel(0);

            // Create displays
            DisplayFactory displayFactory = new DisplayFactory(display);
            displayFactory.CreateCullDisplays();
            displayFactory.CreateDrawDisplays();

#if UNITY_WSA
            if (holoLens.cullMesh) {
                SpatialMappingRenderer renderer = transform.parent.gameObject.AddComponent<SpatialMappingRenderer>();
                renderer.occlusionMaterial = Resources.Load<Material>("CullHoloLens");
            }
            // TODO: may need a material
#endif

            Update();
        }

        public void Update() {
            // Try remoting if the holoLens is not set up
            if (!_holoLensStarted)
#if UNITY_WSA
                _holoLensStarted = TryRemoting();
#else
                _holoLensStarted = true;
#endif

            // Look for an origin if one has not been set
            else {

                _origin.Update();

                if (_input == null) {
                    if (_origin.found)
                        _input = new InputController(this);
                }
                else
                    _input.SendInput();
            }
        }

#if UNITY_WSA

#region Remoting

        // Try starting remoting if neccessary.
        private bool TryRemoting() {
            // Get the remote
            var remote = display.HoloLensData().remote;

            // If remoting has now connected
            if (remote != null && remote.connected == false
                && HolographicRemoting.ConnectionState == HolographicStreamerConnectionState.Connected) {
                remote.connected = true;
                UniSAConfig.current.StartCoroutine(LoadDevice("WindowsMR"));
                SetUpHoloLens();
                return true;
            }

            return false;
        }

        // Neccessary for remoting.
        IEnumerator LoadDevice(string newDevice) {
            XRSettings.LoadDeviceByName(newDevice);
            yield return null;
            XRSettings.enabled = true;
        }
#endregion

        /// <summary>
        /// Adds to the world anchor data.
        /// </summary>
        /// <param name="data">A world anchor as data.</param>
        public void ShareOriginData(byte[] data) {
            if (_originData == null) _originData = new MemoryStream();

            _originData.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Finishes setting the origin.
        /// </summary>
        /// <param name="data">A world anchor as data.</param>
        public void ShareOriginComplete(bool succeeded) {

            if (succeeded && _originData != null) {
                _origin.SetOriginWithData(_originData.ToArray());
            }
            _originData = null;
        }
#endif
    }
}