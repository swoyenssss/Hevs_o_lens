using UnityEngine;

namespace HEVS.UniSA.HoloLens
{
    internal interface OriginFinder
    {
        bool TryGetOrigin(out Vector3 position, out Quaternion rotation);

        void Disable();
    }
}