using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HEVS.UniSA
{

    public static class SWAExtensions
    {
        /// <summary>
        /// This is used because we cannot add holoLensConfig to a display object.
        /// </summary>
        public static HoloLensData HoloLensData(this DisplayConfig displayConfig)
        {
            if (UniSAConfig.current.holoLensConfigs.ContainsKey(displayConfig))
                return UniSAConfig.current.holoLensConfigs[displayConfig];
            return null;
        }

        /// <summary>
        /// This is used because we cannot get the camera a display uses.
        /// </summary>
        public static UnityEngine.Camera Camera(this DisplayConfig config)
        {
            string cameraName = config.id + "-Camera";

            foreach (UnityEngine.Camera camera in GameObject.FindObjectsOfType<UnityEngine.Camera>())
                if (camera.gameObject.name == cameraName)
                    return camera.GetComponent<UnityEngine.Camera>();

            return null;
        }

        /// <summary>
        /// This is used because we cannot get the camera a display uses.
        /// </summary>
        public static Mesh CreateMesh(this DisplayConfig config)
        {
            Mesh mesh = new Mesh();

            switch (config.type)
            {
                case DisplayType.Standard:
                    break;
                case DisplayType.OffAxis:
                    {
                        OffAxisDisplayData data = (OffAxisDisplayData)config.displayData;

                        mesh.vertices = new Vector3[] { data.ul, data.ur, data.lr, data.ll };
                        mesh.triangles = new int[] { 0, 1, 2, 2, 3, 0 };
                    }
                    break;
                case DisplayType.Dome:
                    throw new System.NotImplementedException();
                case DisplayType.Curved:
                    throw new System.NotImplementedException();
                case DisplayType.AVIE:
                    throw new System.NotImplementedException();
            }

            return mesh;
        }
    }
}