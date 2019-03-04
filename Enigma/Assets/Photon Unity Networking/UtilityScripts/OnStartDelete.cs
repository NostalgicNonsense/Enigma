using UnityEngine;

namespace Photon_Unity_Networking.UtilityScripts
{
    /// <summary>This component will destroy the GameObject it is attached to (in Start()).</summary>
    public class OnStartDelete : MonoBehaviour 
    {
        // Use this for initialization
        void Start()
        {
            Destroy(this.gameObject);
        }
    }
}