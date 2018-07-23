using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Enigma.Enums;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Turrets
{
    public abstract class TurretBase : MonoBehaviour
    {
        public Collider ColliderSphere;
        protected GameObject Target;

        public void FixedUpdate()
        {
            if (Target != null)
            {
                Attack();
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (Target == null && IsValidTarget(collision))
            {
                Target = collision.gameObject;
            }
        }

        private bool IsValidTarget(Collision collision)
        {
            return IsCorrectTargetType(collision.transform.gameObject) && TurretHasLineOfSight(collision.transform);
        }

        public void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject == Target)
            {
                Target = null;
            }
        }

        private bool TurretHasLineOfSight(Transform target)
        {
            if (Physics.Linecast(transform.position, target.position))
            {
                return false;
            }

            return true;
        }


        protected abstract bool IsCorrectTargetType(GameObject gameObject);

        protected abstract void Attack();
    }
}
