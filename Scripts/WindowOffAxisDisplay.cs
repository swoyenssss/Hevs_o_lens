﻿using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.Linq;

namespace HEVS.UniSA
{

    [CustomDisplay("trackedwindow")]
    public class TrackedWindowOffAxisDisplay : Tracked<WindowOffAxisDisplay> { }

    [CustomDisplay("window")]
    public class WindowOffAxisDisplay : Display {

        /// <summary>
        /// The basic display.
        /// </summary>
        public OffAxisDisplay display { get => _display; }
        private OffAxisDisplay _display = new OffAxisDisplay();

        /// <summary>
        /// The origin position of the camera
        /// </summary>
        public TransformConfig cameraOrigin { get; }

        /// <summary>
        /// If objects infront of the camera should be visible;
        /// </summary>
        public bool cullInfront { get => _cullInfront; }
        private bool _cullInfront = false;

        public void DrawGizmo(DisplayConfig displayOwner) {
            _display.DrawGizmo(displayOwner);
        }

        /// <summary>
        /// Parse and use OffAxisDisplay's Parse.
        /// </summary>
        public bool Parse(JSONNode jsonNode)
        {
            _display.Parse(jsonNode);
            
            if (jsonNode["cull_infront"] != null) _cullInfront = jsonNode["cull_infront"].AsBool;
            if (jsonNode["camera_origin"] != null) { cameraOrigin.Parse(jsonNode["camera_origin"]); }

            return true;
        }

        /// <summary>
        /// Use OffAxisDisplay's Raycast.
        /// </summary>
        public bool Raycast(DisplayConfig displayOwner, Ray ray, out float distance, out Vector2 hitPoint2D) {
            return _display.Raycast(displayOwner, ray, out distance, out hitPoint2D);
        }

        /// <summary>
        /// Setup and use OffAxisDisplay's Setup.
        /// </summary>
        public void Setup(DisplayConfig displayOwner, bool stereo) {
            displayOwner.displayRig = _display;
            _display.Setup(displayOwner, stereo);

            var camera = Camera.main.GetComponentsInChildren<UnityEngine.Camera>().FirstOrDefault(i => i.name == displayOwner.id);

            if (camera != null) {
                // TODO: this is bad... should not have to do this
                camera.transform.localPosition = Vector3.zero;

                Configuration.OnPreUpdate += () => {

                    // Move the camera
                    // TODO: Check with Conan about this
                    //if (!cameraOrigin.matrix.isIdentity)
                    //    camera.transform.localPosition = displayOwner.transformOffset.TransformPoint(cameraOrigin.translate);
                    
                    // Cull infront
                    if (cullInfront)
                        camera.nearClipPlane = Vector3.Distance(displayOwner.transformOffset.translate, camera.transform.localPosition);
                };
            }
        }

        /// <summary>
        /// Use OffAxisDisplay's ViewportPointToRay.
        /// </summary>
        public Ray ViewportPointToRay(DisplayConfig displayOwner, Vector2 displaySpacePoint) {
            return _display.ViewportPointToRay(displayOwner, displaySpacePoint);
        }
    }
}