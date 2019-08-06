using UnityEngine;
#if UNITY_WSA
using UnityEngine.XR.WSA;
#endif

namespace HEVS.UniSA
{
    // TODO: Would be cool if this could also use a mouse

    /// <summary>
    /// Used to track the position of the cursor in real world space.
    /// </summary>
    internal class CursorLocator
    {

        private TransformConfig _transform;
        /// <summary>
        /// The current transform of the cursor.
        /// </summary>
        public TransformConfig transform { get
            {
                UpdateTransform();
                return _transform;
            }
        }

        // The HoloLens' transform.
        private Transform _holoLens;

#if UNITY_WSA
        // The spatial mapping collider for the HoloLens.
        private SpatialMappingCollider _collider;
#endif

        // The distance from the HoloLens to the cursor.
        private float _distance;

        public CursorLocator(Transform holoLens)
        {
            _holoLens = holoLens;
#if UNITY_WSA
            _collider = new GameObject("Spatial Collider").AddComponent<SpatialMappingCollider>();
#endif
        }

        private void UpdateTransform()
        {
            // Raycast to hit the spatial mesh
            RaycastHit[] hits = Physics.RaycastAll(_holoLens.position, _holoLens.forward);
            if (hits != null && hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    // If this is the surface 
                    if (hit.transform.parent != null && hit.transform.parent.name.StartsWith("Surface Parent"))// TODO: this is bad but we can't make new layers
                    {
                        // Store the distance, as it is more useful than position
                        _distance = Vector3.Distance(_holoLens.position, hit.point);
                        _transform.rotate = Quaternion.LookRotation(hit.normal, _holoLens.up);
                        break;
                    }
                }
            }

            // Update the markers transform
            _transform.translate = _holoLens.position + _holoLens.forward * _distance;
        }

        /// <summary>
        /// Called to destroy the spatial mapping collider
        /// </summary>
        public void Close()
        {
#if UNITY_WSA
            Object.Destroy(_collider.gameObject);
#endif
        }
    }
}