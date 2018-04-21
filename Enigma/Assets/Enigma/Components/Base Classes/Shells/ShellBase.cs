using Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts;
using Assets.Enigma.Components.Basic_Items;
using Assets.Enigma.Enums;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Shells
{
    public class ShellBase : MonoBehaviour
    {
        public Rigidbody ShellBody;
        public float DamageToInflict;

        public void OnCollision(Collision collision)
        {
            if (collision.gameObject.tag == GameEntityType.Vehicle.ToString() ||
                collision.gameObject.tag == GameEntityType.Structure.ToString())
            {
                var DamageHandler = collision.gameObject.GetComponentInChildren<VehicleDamageHandler>();
                DamageHandler.TakeDamage(DamageToInflict);
                Instantiate<BasicExplosion>(this.transform);
            }
            else if (collision.gameObject.tag == GameEntityType.Player.ToString())
            {
                Destroy(collision.gameObject); //TODO: make this better
            }
            Destroy(this);
        }
    }
}
