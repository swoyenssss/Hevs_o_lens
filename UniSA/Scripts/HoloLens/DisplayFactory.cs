using System;
using UnityEngine;

namespace HEVS.UniSA.HoloLens
{

    /// <summary>
    /// Used to create cull and draw displays for HoloLens.
    /// </summary>
    internal class DisplayFactory
    {

        /// <summary>
        /// The display with the holoLens
        /// </summary>
        public DisplayConfig display { get; }
        
        public DisplayFactory(DisplayConfig display) {
            this.display = display;

            // Must have holoLens data
            if (display.HoloLensData() == null)
                throw new Exception("DisplayFactory's \"display\" that has HoloLensData.");
        }

        /// <summary>
        /// Create all the cull displays for the holoLens.
        /// </summary>
        public void CreateCullDisplays()
        {
            HoloLensData holoLens = display.HoloLensData();

            // Create a parent object for cull displays
            Transform cullParent = new GameObject("Trackers").transform;
            cullParent.hideFlags = HideFlags.HideInHierarchy;
            cullParent.parent = UnityEngine.Camera.main.transform.parent;

            // Use the holoLens' transform
            cullParent.localPosition = display.transform.translate;
            cullParent.localRotation = display.transform.rotate;
            cullParent.localScale = display.transform.scale;

            // Create each cull displays
            foreach (DisplayConfig display in holoLens.cullDisplays)
                RenderDisplay(display, Resources.Load<Material>("CullHoloLens")).transform.parent = cullParent;
        }

        /// <summary>
        /// Create all the draw displays for the holoLens.
        /// </summary>
        public void CreateDrawDisplays()
        {
            HoloLensData holoLens = display.HoloLensData();

            Transform drawParent = UnityEngine.Camera.main.transform.parent;

            // Draw Displays
            foreach (DisplayConfig display in holoLens.drawDisplays)
                RenderDisplay(display, Resources.Load<Material>("DrawDisplay")).transform.parent = drawParent;
        }

        // Render a display with a material
        private GameObject RenderDisplay(DisplayConfig display, Material material)
        {
            GameObject gameObject = new GameObject(display.id + "-Display");

            // Track the transform of the display
            DisplayTransform displayTracker = gameObject.AddComponent<DisplayTransform>();
            displayTracker.displayID = display.id;

            // Create the renderer and filter
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = material;
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = display.CreateMesh();

            return gameObject;
        }
    }
}