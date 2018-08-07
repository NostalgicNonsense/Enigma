using Assets.Enigma.Enums;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Turrets
{
    public class GunTurret : TurretBase
    {
        public Collider GunCollider;
        public TurretGun Gun;

        public void Start()
        {
            Gun.SetTeam(TeamOfTurret);
        }

        protected override void Attack(GameObject target)
        {
            GunCollider.transform.LookAt(target.transform);
            Gun.FireAtTarget(target);
        }
    }
}
