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

        public void FixedUpdate()
        {
            RaycastHit rayCast;
            Debug.DrawRay(transform.position, transform.forward, Color.magenta, GetComponent<Rigidbody>().velocity.magnitude * 100f);
            if (Physics.Raycast(transform.position, transform.forward, out rayCast,
                GetComponent<Rigidbody>().velocity.magnitude * 100f))
            {
                if (rayCast.collider.gameObject.tag == GameEntityType.Vehicle.ToString() ||
                    rayCast.collider.gameObject.tag == GameEntityType.Structure.ToString())
                {
                    var damageHandler = rayCast.collider.gameObject.GetComponentInChildren<VehicleDamageHandler>();
                    damageHandler.TakeDamage(DamageToInflict);
                }
                else if (rayCast.collider.gameObject.tag == GameEntityType.Player.ToString())
                {
                    Destroy(rayCast.collider.gameObject); //TODO: make this better
                }
                ExplodeAndTerminate();
            }
        }

        private void ExplodeAndTerminate()
        {
            var explosionInstance = Instantiate(ExplosionToUse);
            explosionInstance.Explode();
            Destroy(gameObject);
            Destroy(this);
        }
    }


}
