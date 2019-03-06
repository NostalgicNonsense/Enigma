using UnityEngine;

namespace Enigma.Components.Gameplay.Vehicle.ComponentScripts
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
