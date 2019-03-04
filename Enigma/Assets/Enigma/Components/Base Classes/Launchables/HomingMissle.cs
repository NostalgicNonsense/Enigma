using System;
using System.Collections;
using Enigma.Components.Base_Classes.Vehicle.ComponentScripts;
using UnityEngine;
using UnityEngine.Networking;

namespace Enigma.Components.Base_Classes.Launchables
{
    public class HomingMissle : NetworkBehaviour
    {
        public float Speed;
        public float FlightTime;
        public float MaximumRadiansOfRotation;
        public float Damage;
        public float InitialForce;
        //public BasicExplosion ExplosionToUse;
        private GameObject _target;
        private DateTime _startTime;
        private bool _timeExceeded;

        public void Start()
        {
            Debug.Log("Instantiating homing missile");
            _startTime = DateTime.UtcNow;
            GetComponent<Rigidbody>().AddForce(transform.forward * InitialForce);
            StartCoroutine(CountDownTime());
        }

        public void OnCollisionEnter(Collision collsion)
        {
            HandleExploding(collsion.gameObject);
        }

        private void HandleExploding(GameObject thingMissileHit)
        {
            //var explosionInstance = Instantiate(ExplosionToUse, transform.position, transform.rotation);
            //explosionInstance.Explode();
            if (thingMissileHit != null)
            {
                thingMissileHit.GetComponent<EnigmaDamageHandler>().TakeDamage(Damage);
            }
            //Destroy(this);
        }

        public void SetTarget(GameObject target)
        {
            _target = target;
        }

        public void FixedUpdate()
        {
            if (_timeExceeded)
            {
                HandleExploding(null);
            }

            var step = Speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, step);
        }

        private IEnumerator CountDownTime()
        {
            yield return new WaitForSeconds(FlightTime);
            _timeExceeded = true;
        }
    }
}
