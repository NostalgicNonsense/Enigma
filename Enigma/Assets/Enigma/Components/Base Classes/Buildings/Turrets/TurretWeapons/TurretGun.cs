using System.Collections;
using Assets.Enigma.Components.Base_Classes.Buildings.Turrets.Targeting;
using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using Assets.Enigma.Components.HelpClasses.Builders;
using Assets.Enigma.Components.HelpClasses.ExtensionMethods;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Turrets.TurretWeapons
{
    public class TurretGun : MonoBehaviour, ITurretWeapon
    {
        //TODO: A turret shouldn't have its own class of weapon. So probably need a refactor.
        public Collider GunCollider;
        public float Inaccuray;
        public float Damage;
        private float _timeToSleep = 60 / 600f;
        private const float Z = 10f; // wtf does this do
        private Team _team;
        private bool _reloading;

        public void FixedUpdate()
        {
            var target = GetComponent<ITargeter>().Target;
            if (target != null)
            {
                GunCollider.transform.LookAt(target.transform);
            }
        }

        private void FireAtTarget(GameObject target)
        {
            var coroutine = HandleFiring(target);
            StartCoroutine(coroutine);
        }

        private IEnumerator HandleFiring(GameObject target)
        {
            if (_reloading)
            {
                yield return null;
            }
            var randomNumberForBurst = Random.Range(4, 9);
            for (var i = 0; i < randomNumberForBurst; i++)
            {
                ShootRay(target);
                yield return new WaitForSeconds(_timeToSleep);
            }

            _reloading = true;
            yield return new WaitForSeconds(Random.Range(2, 5.5f));
            _reloading = false;
        }

        private void ShootRay(GameObject target)
        {
            var directionToTarget = GetDirectionToTarget(target.transform);
            var ray = GetInaccurateRay(directionToTarget.GetPlayerAdjustedVector3());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                HandleHit(hit);
            }
        }

        private Ray GetInaccurateRay(Vector3 directionToTarget)
        {
            return InaccurateRayBuilder.GetInaccurateRay(transform.position, directionToTarget, Inaccuray);
        }

        private void HandleHit(RaycastHit hit)
        {
            var playerHit = hit.collider.gameObject.GetComponentInChildren<vp_CharacterController>();
            var damageHandler = playerHit.CharacterController.GetComponent<vp_DamageHandler>();
            damageHandler.Damage(Damage);
        }

        private Vector3 GetDirectionToTarget(Transform targeTransform)
        {
            return targeTransform.position - transform.position;
        }

        public void Attack(GameObject target)
        {
            FireAtTarget(target);
        }
    }
}
