using System;
using Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts;
using Assets.Enigma.Components.Basic_Items;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Enigma.Components.Base_Classes.Launchables
{
    public class HomingMissle : NetworkBehaviour
    {
        public float Speed;
        public float FlightTime;
        public float RotationSpeed;
        public float Damage;
        public BasicExplosion ExplosionToUse;
        private GameObject _target;
        private DateTime _startTime;

        public void Start()
        {
            _startTime = DateTime.UtcNow;
            GetComponent<Rigidbody>().AddForce(transform.forward * 12000f);
        }

        public void OnCollisionEnter(Collision collsion)
        {
            var explosionInstance = Instantiate(ExplosionToUse, transform.position, transform.rotation);
            explosionInstance.Explode();
            collsion.gameObject.GetComponent<EnigmaDamageHandler>().TakeDamage(Damage);
            Destroy(this);
        }

        public void SetTarget(GameObject target)
        {
            _target = target;
        }

        public void FixedUpdate()
        {
            var timeElapsed = (_startTime - DateTime.UtcNow).TotalSeconds;
            if (timeElapsed > FlightTime)
            {
                // end;
            }
            var newRotation = Vector3.RotateTowards(transform.forward, _target.transform.position, RotationSpeed, 0.0f);
            Debug.DrawRay(transform.position, newRotation, Color.green);

            transform.rotation = Quaternion.LookRotation(newRotation);
        }
    }
}
