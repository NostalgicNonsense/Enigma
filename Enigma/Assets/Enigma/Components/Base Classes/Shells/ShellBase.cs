using Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts;
using Assets.Enigma.Components.Basic_Items;
using Assets.Enigma.Enums;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Shells
{
    public class ShellBase : MonoBehaviour
    {
        public BasicExplosion ExplosionToUse;
        public float DamageToInflict;

        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == GameEntityType.Vehicle.ToString() ||
                collision.gameObject.tag == GameEntityType.Structure.ToString())
            {
                var DamageHandler = collision.gameObject.GetComponentInChildren<VehicleDamageHandler>();
                DamageHandler.TakeDamage(DamageToInflict);
            }
            else if (collision.gameObject.tag == GameEntityType.Player.ToString())
            {
                Destroy(collision.gameObject); //TODO: make this better
            }
            var explosionInstance = Instantiate(ExplosionToUse);
            explosionInstance.Explode();
            Destroy(gameObject);
        }
    }
}
