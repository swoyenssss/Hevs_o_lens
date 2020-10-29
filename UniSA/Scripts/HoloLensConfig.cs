using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using UnityEngine.EventSystems;

namespace HEVS.UniSA.HoloLens
{

    /// <summary>
    /// Adds HoloLens functionality to HEVS.
    /// </summary>
    public class HoloLensConfig : MonoBehaviour
    {

        #region Variables
        /// <summary>
        /// The main extra config being used by all other classes.
        /// </summary>
        public static HoloLensConfig current;

        /// <summary>
        /// The port for sending holoLens 6Dof
        /// </summary>
        [HideInInspector]
        public int holoPort = 6668;

        /// <summary>
        /// Links display id's to their HoloLens Config.
        /// </summary>
        internal Dictionary<DisplayConfig, HoloLensData> holoLensConfigs;

        internal HoloLensDevice holoLens;
        
        #endregion

        #region Unity Methods

        private void Start()
        {
            current = this;

            // Read in all neccessary data
            ReadHoloLensConfigs();

            // Get the Holoport if it exists
            if (PlatformConfig.current.globals.ContainsKey("holo_port"))
                holoPort = (int)PlatformConfig.current.globals["holo_port"];

            #region Set Up HoloLens

            // Only create singleton if it will be used
            if (holoLensConfigs != null)
            {
                if (Cluster.isMaster) HoloLensMaster.current = new HoloLensMaster();

                // Client must have a holoLens device
                foreach (DisplayConfig display in NodeConfig.current.displays)
                {
                    if (holoLensConfigs.ContainsKey(display))
                    {
                        holoLens = new HoloLensDevice(UnityEngine.Camera.main.transform, display);
                        break;
                    }
                }
            }
            #endregion
        }

        private void Update()
        {
            if (holoLens != null) holoLens.Update();
        }

        private void OnApplicationQuit() {
            Transmitter.CloseAll();
        }
        #endregion

        // Loads the HoloLens configs from the json
        private void ReadHoloLensConfigs()
        {
            // For creating the trackers
            var holoLensConfigs = new Dictionary<DisplayConfig, HoloLensData>();

            // Check every display of the platform
            foreach (DisplayConfig display in PlatformConfig.current.displays)
            {
                if (display.json == null) { continue; }
                JSONNode typeJSON = display.json["type"];

                if (typeJSON != null && typeJSON.Value.ToLower() == "hololens")
                {
                    HoloLensData holoLens = new HoloLensData();
                    holoLensConfigs.Add(display, holoLens);
                    
                    holoLens.ParseJson(display.json);
                }
            }

            if (holoLensConfigs.Count > 0) { this.holoLensConfigs = holoLensConfigs; }
        }

        [HEVS.RPC]
        internal void ShareOriginData(byte[] data) {
#if UNITY_WSA
            if (holoLens != null) holoLens.ShareOriginData(data);
#endif
            if (Cluster.isMaster) RPCManager.CallClient(this, "ShareOriginData", data);
        }

        [HEVS.RPC]
        internal void ShareOriginComplete(bool succeeded) {
#if UNITY_WSA
            if (holoLens != null) holoLens.ShareOriginComplete(succeeded);
#endif
            if (Cluster.isMaster) RPCManager.CallClient(this, "ShareOriginComplete", succeeded);
        }
    }
}