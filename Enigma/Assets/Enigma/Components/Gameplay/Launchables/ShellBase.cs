using Enigma.Components.Gameplay.Vehicle.ComponentScripts;
using Enigma.Components.Gibs;
using Enigma.Enums;
using UnityEngine;

namespace Enigma.Components.Gameplay.Launchables
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
                    var damageHandler = rayCast.collider.gameObject.GetComponentInChildren<EnigmaDamageHandler>();
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
            var explosionInstance = Instantiate(ExplosionToUse, transform.position, transform.rotation);
            explosionInstance.Explode();
            Destroy(gameObject);
            Destroy(this);
        }
    }
}
