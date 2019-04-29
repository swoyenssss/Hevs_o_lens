using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HEVS;

public class TransformDisplay : MonoBehaviour
{
    // The id of the display to update
    public List<string> displayIDs;
    private StoredDisplay[] _displays;

    // Start is called before the first frame update
    void Start()
    {
        // Get all the displays using IDs
        _displays = displayIDs.Select(i => new StoredDisplay(NodeConfig.current.displays.First(j => j.id == i))).ToArray();

        // Update the position and rotation
        UpdateDisplays();
    }

    // Update is called once per frame
    void Update()
    {
        // Update the position and rotation
        UpdateDisplays();
    }

    /// <summary>
    /// Update displays to match the transform.
    /// </summary>
    public void UpdateDisplays()
    {
        foreach (StoredDisplay display in _displays)
            display.Update(transform);
    }

    /// <summary>
    /// Holds a display config and its initial transform.
    /// </summary>
    private class StoredDisplay
    {
        /// <summary>
        /// The connected display.
        /// </summary>
        public DisplayConfig config;

        /// <summary>
        /// The original transform of the display.
        /// </summary>
        public TransformData originalTransform;

        /// <summary>
        /// Constructs a stored display, storing the original transform.
        /// </summary>
        /// <param name="config">The connected display.</param>
        public StoredDisplay(DisplayConfig config)
        {
            this.config = config;
            originalTransform = config.transform;

            // TODO: Remove this
            StartOffAxis();
        }

        /// <summary>
        /// Updates the display to use a game objects transfrom.
        /// </summary>
        /// <param name="transform">The transform to apply</param>
        public void Update(Transform transform)
        {
            DisplayTrackerConfig constraints = config.TrackerConfig();

            // Get the position ignoring locks
            var translate = transform.position;
            config.transform.translate = originalTransform.translate + new Vector3(
                constraints.translateX ? translate.x : 0f,
                constraints.translateY ? translate.y : 0f,
                constraints.translateZ ? translate.z : 0f);

            // Get the rotation ignoring locks
            var euler = transform.rotation.eulerAngles;
            config.transform.rotate = originalTransform.rotate * Quaternion.Euler(
                constraints.rotateX ? euler.x : 0f,
                constraints.rotateY ? euler.y : 0f,
                constraints.rotateZ ? euler.z : 0f);

            // TODO: Remove this
            UpdateOffAxis();
        }
        
        // TODO: remove this when OffAxis works
        #region OffAxis
        public Vector3 originalUL;
        public Vector3 originalLL;
        public Vector3 originalLR;

        private void StartOffAxis()
        {
            if (config.type == DisplayType.OffAxis)
            {
                originalUL = config.offAxisData.ul;
                originalLL = config.offAxisData.ll;
                originalLR = config.offAxisData.lr;
            }
        }

        private void UpdateOffAxis()
        {
            if (config.type == DisplayType.OffAxis)
            {
                config.offAxisData.ul = config.transform.rotate
                    * (originalUL + config.transform.translate);
                config.offAxisData.ll = config.transform.rotate
                    * (originalLL + config.transform.translate);
                config.offAxisData.lr = config.transform.rotate
                    * (originalLR + config.transform.translate);
            }
        }
        #endregion
    }
}