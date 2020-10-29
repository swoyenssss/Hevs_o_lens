using System;
using System.Collections;
using UnityEngine;
using HEVS.UniSA.HoloLens;
#if UNITY_WSA
using UnityEngine.XR;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Persistence;
using UnityEngine.XR.WSA.Sharing;
#endif

namespace HEVS.UniSA.HoloLens {

    internal class OriginController {
        #region Variables

        /// <summary>
        /// The HoloLens Device the controller is for.
        /// </summary>
        public HoloLensDevice holoLens { get; }

        /// <summary>
        /// True if the origin has been found.
        /// </summary>
        public bool found { get; private set; }

        // Used to find the origin
        private OriginFinder _originFinder;

#if UNITY_WSA
        // The world anchor for saving and storing world anchors
        private WorldAnchorStore _worldAnchorStore;

        // The world anchor to use
        private WorldAnchor _worldAnchor;
#endif

        #endregion

        /// <summary>
        /// Constructs an origin controller object.
        /// </summary>
        /// <param name="holoLens">The holoLens device.</param>
        public OriginController(HoloLensDevice holoLens) {
            this.holoLens = holoLens;

#if UNITY_WSA
            // Load world anchors store
            WorldAnchorStore.GetAsync(WorldAnchorStoreLoaded);
#else
            StartFinding();
#endif
        }

        /// <summary>
        /// Updates the Origin Controller.
        /// </summary>
        public void Update() {
            if (!found && _originFinder != null) {
                // If the origin finder worked
                if (_originFinder.TryGetOrigin(out Vector3 position, out Quaternion rotation)) {
#if UNITY_WSA
                    SetWorldAnchor(CreateWorldAnchor(position, rotation));
#else
                    SetOrigin(position, rotation);
#endif
                }
            }
        }

        // Starts searching for the origin
        private void StartFinding() {
            // Set the origin finder
            switch (holoLens.display.HoloLensData().origin) {

#if UNITY_WSA
                case HoloLensData.OriginType.CHOOSE_ORIGIN:
                _originFinder = new OriginPointer(holoLens.transform);
                return;
#endif

                case HoloLensData.OriginType.FIND_MARKER:
                _originFinder = new OriginLocator(holoLens.transform);
                return;

                default:
#if UNITY_WSA
                SetWorldAnchor(CreateWorldAnchor(holoLens.transform.position, Quaternion.Euler(0f, holoLens.transform.localEulerAngles.y, 0f)));
#else
                SetOrigin(holoLens.transform.position, Quaternion.Euler(0f, holoLens.transform.localEulerAngles.y, 0f));
#endif
                return;
            }
        }

#if UNITY_WSA

        #region Set World Anchor

        // Sets the origin and direction for the holoLens
        private void SetWorldAnchor(WorldAnchor worldAnchor) {
            if (worldAnchor == null) { return; }
            _worldAnchor = worldAnchor;

            SetOrigin(worldAnchor.transform.position, worldAnchor.transform.rotation);

            if (_worldAnchorStore != null)
                _worldAnchorStore.Save(holoLens.display.id, _worldAnchor);

            found = true;
        }

        // Sets the origin and creates a world anchor
        private WorldAnchor CreateWorldAnchor(Vector3 position, Quaternion rotation) {
            // Create world anchor
            GameObject anchorObject = new GameObject("Origin");
            anchorObject.transform.position = position;
            anchorObject.transform.rotation = rotation;

            WorldAnchor worldAnchor = anchorObject.AddComponent<WorldAnchor>();

#if !UNITY_EDITOR
            if (holoLens.display.HoloLensData().shareOrigin)
            {
                // Share the world anchor
                WorldAnchorTransferBatch transferBatch = new WorldAnchorTransferBatch();
                transferBatch.AddWorldAnchor(holoLens.display.id, worldAnchor);

                WorldAnchorTransferBatch.ExportAsync(transferBatch, (byte[] data) => {
                    RPCManager.CallMaster(HoloLensConfig.current, "ShareOriginData", data);
                }, (SerializationCompletionReason completionReason) => {
                    RPCManager.CallMaster(HoloLensConfig.current, "ShareOriginComplete", completionReason == SerializationCompletionReason.Succeeded);
                });
            }
#endif

            return worldAnchor;
        }
        #endregion

        #region Shared World Anchor

        /// <summary>
        /// Sets the world anchor at the origin from data.
        /// </summary>
        /// <param name="data">A world anchor as data.</param>
        public void SetOriginWithData(byte[] data) {

            if (holoLens.display.HoloLensData().shareOrigin) {
                WorldAnchorTransferBatch.ImportAsync(data, (SerializationCompletionReason completionReason, WorldAnchorTransferBatch batch) => {

                    if (completionReason != SerializationCompletionReason.Succeeded) return;

                    // Disable the origin finder if it exists
                    if (_originFinder != null) {
                        _originFinder.Disable();
                        _originFinder = null;
                    }

                    // Create the world anchor
                    SetWorldAnchor(batch.LockObject(holoLens.display.id, _worldAnchor == null ?
                        new GameObject("WorldAnchor") : _worldAnchor.gameObject));
                });
            }
        }

        #endregion

        #region Store World Anchor

        // Once the store is loaded, try to load the world anchor
        private void WorldAnchorStoreLoaded(WorldAnchorStore store) {
            _worldAnchorStore = store;

            if (holoLens.display.HoloLensData().usePreviousOrigin) {
                WorldAnchor worldAnchor = _worldAnchorStore.Load(holoLens.display.id, new GameObject("WorldAnchor"));

                if (worldAnchor) {
                    SetWorldAnchor(worldAnchor);
                    return;
                }
            }

            // Origin could already be found if sharing was used
            if (_worldAnchor)
                _worldAnchorStore.Save(holoLens.display.id, _worldAnchor);
            else
                StartFinding();
        }
        #endregion

#endif

        // Sets the origin and direction for the holoLens
        private void SetOrigin(Vector3 position, Quaternion rotation) {

            Transform container = holoLens.transform.parent;
            Transform transform = container.parent;

            // Reverse the current transform
            container.position = -position;

            // Update container transform
            transform.position = holoLens.display.transform.translate;
            transform.rotation = holoLens.display.transform.rotate * Quaternion.Inverse(rotation);
            transform.localScale = holoLens.display.transform.scale;

            // Disable the origin finder
            if (_originFinder != null) {
                _originFinder.Disable();
                _originFinder = null;
            }

            found = true;
        }
    }
}