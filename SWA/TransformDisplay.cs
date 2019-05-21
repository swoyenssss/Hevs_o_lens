using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HEVS;

public class TransformDisplay : MonoBehaviour
{
    /// <summary>
    /// The id of the display to update.
    /// </summary>
    public List<string> displayIDs;

    // The actual disply and its original transform
    private StoredDisplay[] _displays;

    // Start is called before the first frame update
    void Start()
    {
        // Get all the displays using IDs
        _displays = displayIDs.Select(i => new StoredDisplay(PlatformConfig.current.displays.Find(j => j.id == i))).ToArray();

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
        /// The display to store.
        /// </summary>
        public DisplayConfig display;

        /// <summary>
        /// The original transform of the display.
        /// </summary>
        public TransformData originalTransform;

        /// <summary>
        /// Constructs a stored display, storing the original transform.
        /// </summary>
        /// <param name="config">The connected display.</param>
        public StoredDisplay(DisplayConfig display)
        {
            this.display = display;
            originalTransform = display.transform;
        }

        /// <summary>
        /// Updates the display to use a game objects transfrom.
        /// </summary>
        /// <param name="transform">The transform to apply</param>
        public void Update(Transform transform)
        {
            // Get the position ignoring locks
            display.transform.translate = originalTransform.translate + transform.position;

            // Get the rotation ignoring locks
            display.transform.rotate = originalTransform.rotate * transform.rotation;
        }
    }
}