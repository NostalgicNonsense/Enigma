using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Enigma.Components.Gibs
{
    public class GibExplosion : MonoBehaviour
    {
        public float explosionForce = 4;
        public List<GameObject> SpawnDeath = new List<GameObject>();

        private IEnumerator Start()
        {
            // wait one frame because some explosions instantiate debris which should then
            // be pushed by physics force
            yield return null;

            float multiplier = 1;

            float r = 10 * multiplier;
            var cols = Physics.OverlapSphere(transform.position, r);
            var rigidbodies = GetComponentsInChildren<Rigidbody>();
            
            if (SpawnDeath != null)
            {
                foreach (var spawn in SpawnDeath)
                {
                    Instantiate(spawn);
                }
            }

            foreach (var rb in rigidbodies)
            {
                rb.AddExplosionForce(explosionForce * multiplier, transform.position, r, 1 * multiplier, ForceMode.Impulse);
            }
        }
    }
}
