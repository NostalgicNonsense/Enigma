using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts
{
    public class VehicleDamageHandler : MonoBehaviour
    {
        public float Health;
        public GameObject Parent;

        public void TakeDamage(float damageToTake)
        {
            Health = -damageToTake;
            if (Health < 0)
            {
                //How do we want to handle dying?
                Destroy(Parent);
                Destroy(this);
            }
        }
    }
}
