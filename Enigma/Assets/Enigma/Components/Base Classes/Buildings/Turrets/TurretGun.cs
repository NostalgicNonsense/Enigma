using System.Collections;
using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Turrets
{
    public class TurretGun : MonoBehaviour
    {
        //TODO: A turret shouldn't have its own class of weapon. So probably need a refactor.
        public float Inaccuray;
        public float Damage;
        private float _timeToSleep = 60 / 600f;
        private const float Z = 10f; // wtf does this do
        private Team _team;

        public void SetTeam(Team team)
        {
            _team = team;
        }

        public void FireAtTarget(GameObject target)
        {
            var coroutine = HandleFiring(target);
            StartCoroutine(coroutine);
        }

        private IEnumerator HandleFiring(GameObject target)
        {
            var randomNumberForBurst = Random.Range(4, 9);
            for (var i = 0; i < randomNumberForBurst; i++)
            {
                ShootRay(target);
                yield return new WaitForSeconds(_timeToSleep);
            }

            yield return new WaitForSeconds(Random.Range(2, 5.5f));
        }

        private void ShootRay(GameObject target)
        {
            var directionToTarget = GetDirectionToTarget(target.transform);
            var ray = GetInaccurateRay(directionToTarget);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                HandleHit(hit);
            }
        }

        private Ray GetInaccurateRay(Vector3 directionToTarget)
        {
            directionToTarget.x = Random.Range(directionToTarget.x - Inaccuray, directionToTarget.x + Inaccuray);
            directionToTarget.y = Random.Range(directionToTarget.y - Inaccuray, directionToTarget.y + Inaccuray);
            return new Ray(transform.position, directionToTarget);
        }

        private void HandleHit(RaycastHit hit)
        {
            var playerHit = hit.collider.gameObject.GetComponentInChildren<vp_CharacterController>();
            var damageHandler = playerHit.CharacterController.GetComponent<vp_DamageHandler>();
            damageHandler.Damage(Damage, _team);
        }

        private Vector3 GetDirectionToTarget(Transform targeTransform)
        {
            return targeTransform.position - transform.position;
        }
    }
}
