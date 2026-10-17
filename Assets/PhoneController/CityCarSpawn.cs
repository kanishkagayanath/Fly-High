using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Optional city car start. Place about 0.8 metres above a clear road.</summary>
    public sealed class CityCarSpawn : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(1.8f, 0.8f, 3.6f));
            Gizmos.DrawRay(transform.position, transform.forward * 4);
        }
    }
}
