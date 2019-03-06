using System.Collections;
using Enigma.Components.Gameplay.Buildings.Turrets.Targeting;
using Enigma.Components.Gameplay.Launchables;
using UnityEngine;

namespace Enigma.Components.Gameplay.Buildings.Turrets.TurretWeapons
{
    public class TurretMissileLauncher : MonoBehaviour, ITurretWeapon
    {
        public GameObject MissilePrefab;
        public GameObject MissileSpawnPoint;
        public Collider TurretCollider;
        public int ReloadTime;
        private bool _reloading;

        public void FixedUpdate()
        {
            var target = GetComponent<ITargeter>().Target;
            if (target != null)
            {
                TurretCollider.transform.LookAt(target.transform);
            }
        }

        public void Attack(GameObject target)
        {
            FireMissilesAtTarget(target);
        }

        private void FireMissilesAtTarget(GameObject target)
        {
            if (_reloading)
            {
                return;
            }

            var missile = Instantiate(MissilePrefab, MissileSpawnPoint.transform.position,
                                      MissileSpawnPoint.transform.rotation);
            missile.GetComponent<HomingMissle>().SetTarget(target);
            missile.GetComponent<HomingMissle>().Start();
            missile.gameObject.SetActive(true);
            StartCoroutine(Reload());
        }

        private IEnumerator Reload()
        {
            _reloading = true;
            yield return new WaitForSeconds(ReloadTime);
            _reloading = false;
        }
    }
}
