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

namespace HEVS.UniSA.HoloLens
{

    internal class OriginController
    {
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
        
        // The world anchor for saving and storing world anchors
        private WorldAnchorStore _worldAnchorStore;

        // The world anchor to use
        private WorldAnchor _worldAnchor;
        #endregion

        /// <summary>
        /// Constructs an origin controller object.
        /// </summary>
        /// <param name="holoLens">The holoLens device.</param>
        public OriginController(HoloLensDevice holoLens)
        {
            this.holoLens = holoLens;

            // Load world anchors or start now
            if (holoLens.display.HoloLensData().storeOrigin)
                WorldAnchorStore.GetAsync(WorldAnchorStoreLoaded);
            else
                StartFinding();

        }

        /// <summary>
        /// Updates the Origin Controller.
        /// </summary>
        public void Update()
        {
            if (!found && _originFinder != null)
            {
                // If the origin finder worked
                if (_originFinder.TryGetOrigin(out Vector3 position, out Quaternion rotation))
                    SetWorldAnchor(CreateWorldAnchor(position, rotation));
            }
        }

        // Starts searching for the origin
        private void StartFinding()
        {
            // Set the origin finder
            switch (holoLens.display.HoloLensData().origin)
            {
                case HoloLensData.OriginType.START_LOCATION:
                    SetWorldAnchor(CreateWorldAnchor(holoLens.transform.position, Quaternion.Euler(0f, -holoLens.transform.localEulerAngles.y, 0f)));
                    return;

                case HoloLensData.OriginType.CHOOSE_ORIGIN:
                    _originFinder = new OriginPointer(holoLens.transform);
                    return;

                case HoloLensData.OriginType.FIND_MARKER:
                    _originFinder = new OriginLocator(holoLens.transform);
                    return;
            }
        }

        #region Set World Anchor

        // Sets the origin and direction for the holoLens
        private void SetWorldAnchor(WorldAnchor worldAnchor)
        {
            if (worldAnchor == null) { return; }
            _worldAnchor = worldAnchor;

            Transform container = holoLens.transform.parent;

            // Reverse the current transform
            container.position = -worldAnchor.transform.position;
            container.rotation = Quaternion.Inverse(worldAnchor.transform.rotation);
            
            // Update container transform
            container.localPosition += holoLens.display.transform.translate;
            container.localRotation *= holoLens.display.transform.rotate;
            container.localScale = holoLens.display.transform.scale;

            // Disable the origin finder
            if (_originFinder != null)
            {
                _originFinder.Disable();
                _originFinder = null;
            }

            if (_worldAnchorStore != null)
                _worldAnchorStore.Save(holoLens.display.id, _worldAnchor);

            found = true;
        }

        // Sets the origin and creates a world anchor
        private WorldAnchor CreateWorldAnchor(Vector3 position, Quaternion rotation)
        {
#if UNITY_WSA
            // Create world anchor
            WorldAnchor worldAnchor = new GameObject("WorldAnchor").AddComponent<WorldAnchor>();
            worldAnchor.transform.position = position;
            worldAnchor.transform.rotation = rotation;

            // TODO: For some reason this crashes things
            /*if (holoLens.display.HoloLensData().shareOrigin)
            {
                // Share the world anchor
                WorldAnchorTransferBatch transferBatch = new WorldAnchorTransferBatch();
                transferBatch.AddWorldAnchor(holoLens.display.id, worldAnchor);
                WorldAnchorTransferBatch.ExportAsync(transferBatch, SendWorldAnchorData, SendComplete);
            }*/

            return worldAnchor;
#else
            return null;
#endif
        }
        #endregion

        #region Shared World Anchor

        /// <summary>
        /// Sets the world anchor at the origin from data.
        /// </summary>
        /// <param name="data">A world anchor as data.</param>
        public void SetOriginWithData(byte[] data)
        {
            if (holoLens.display.HoloLensData().shareOrigin)
                WorldAnchorTransferBatch.ImportAsync(data, ReceiveWorldAnchor);
        }

        // Share bytes with other HoloLenses
        private void SendWorldAnchorData(byte[] data)
        {
            RPCManager.CallMaster(HoloLensConfig.current, "SetAllHoloLensOrigins", data);
        }

        private void SendComplete(SerializationCompletionReason completionReason) { }
        
        // Get world anchor from byte data
        private void ReceiveWorldAnchor(SerializationCompletionReason completionReason, WorldAnchorTransferBatch deserializedTransferBatch)
        {
            if (completionReason != SerializationCompletionReason.Succeeded) return;

            // Disable the origin finder if it exists
            if (_originFinder != null)
            {
                _originFinder.Disable();
                _originFinder = null;
            }

            // Create the world anchor
            SetWorldAnchor(deserializedTransferBatch.LockObject(holoLens.display.id, _worldAnchor == null ?
                new GameObject("WorldAnchor") : _worldAnchor.gameObject));
        }
        #endregion

        #region Store World Anchor

        // Once the store is loaded, try to load the world anchor
        private void WorldAnchorStoreLoaded(WorldAnchorStore store)
        {
            _worldAnchorStore = store;

            WorldAnchor worldAnchor = _worldAnchorStore.Load(holoLens.display.id, new GameObject("WorldAnchor"));

            if (worldAnchor)
                SetWorldAnchor(worldAnchor);
            else
            {
                // Origin could be found if sharing was used
                if (_worldAnchor)
                    _worldAnchorStore.Save(holoLens.display.id, _worldAnchor);
                else
                    StartFinding();
            }
        }
        #endregion
    }
}