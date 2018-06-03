using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts
{
    public class VehicleDamageHandler : MonoBehaviour
    {
        public float Health;

        void Start()
        {
        }

        public void TakeDamage(float damageToTake)
        {
            Health = -damageToTake;
            if (Health < 0)
            {
                //How do we want to handle dying?
                var vehicle = GetComponent<SimpleCarController>();
                vehicle.EjectPassangers();
                //Destroy(ParentVehicle);
                Destroy(this);
            }
        }
    }
}
