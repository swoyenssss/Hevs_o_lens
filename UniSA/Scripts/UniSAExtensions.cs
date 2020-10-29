using UnityEngine;
using HEVS.UniSA.HoloLens;

namespace HEVS.UniSA
{

    internal static class SWAExtensions
    {
        /// <summary>
        /// This is used because we cannot add holoLensConfig to a display object.
        /// </summary>
        public static HoloLensData HoloLensData(this DisplayConfig displayConfig)
        {
            if (HoloLensConfig.current.holoLensConfigs.ContainsKey(displayConfig))
                return HoloLensConfig.current.holoLensConfigs[displayConfig];
            return null;
        }

        /// <summary>
        /// This is used because we cannot get the camera a display uses.
        /// </summary>
        public static Transform Transform(this DisplayConfig config)
        {
            if (config.cameraParent.name.StartsWith(config.id) || config.cameraParent.childCount == 0)
                return config.cameraParent;

            foreach (Transform child in config.cameraParent)
                if (child.name.StartsWith(config.id))
                    return child;

            return null;
        }

        /// <summary>
        /// Gets the json node of a tracker.
        /// </summary>
        public static SimpleJSON.JSONNode Json(this TrackerConfig config)
        {
            foreach (var json in PlatformConfig.current.json["trackers"].Children)
                if (config.id == json["id"]) return json;

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
                    // Standard displays do not have meshes
                    break;
                case DisplayType.OffAxis:
                    {
                        OffAxisDisplayData data = (OffAxisDisplayData)config.displayData;

                        mesh.vertices = new Vector3[] { data.ul, data.ur, data.lr, data.ll };
                        mesh.triangles = new int[] { 0, 1, 2, 2, 3, 0,
                                                     0, 2, 1, 2, 0, 3};
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