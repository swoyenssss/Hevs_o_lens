using System.Collections.Generic;
using UnityEngine;

namespace HEVS.UniSA {
    /// <summary>
    /// Adds the following functionality:
    ///     - Mixed reality displays
    ///     - Generic tracked displays
    ///     - Fixed origin offaxis displays
    /// </summary>
    public class UniSAConfig : MonoBehaviour {

        public static UniSAConfig current { get => _current; }
        private static UniSAConfig _current;

        private void Awake() {
            _current = this;

            // Eventually won't need this
            HoloLensMaster.current = new HoloLensMaster();
        }

        private void OnApplicationQuit() {
            Transmitter.CloseAll();
            HoloLensMaster.current.Disable();
        }


        [HEVS.RPC]
        private void ShareOriginData(string clusterID, byte[] data) {
#if UNITY_WSA
            OriginController.ShareOriginData(clusterID, data);
#endif
        }

        [HEVS.RPC]
        private void ShareOriginComplete(string clusterID, bool succeeded) {
#if UNITY_WSA
            OriginController.ShareOriginComplete(clusterID, succeeded);
#endif
        }
    }
}