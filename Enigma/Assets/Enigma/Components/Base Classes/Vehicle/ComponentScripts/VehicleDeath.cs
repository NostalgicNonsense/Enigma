using UnityEngine;

namespace Enigma.Components.Base_Classes.Vehicle.ComponentScripts
{
    public class VehicleDeath : MonoBehaviour
    {
        public GameObject SpawnOnDeath;

        public void Die()
        {
            Instantiate(SpawnOnDeath);
        }
    }
}
