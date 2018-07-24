using Assets.Enigma.Enums;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Turrets
{
    public class GunTurret : TurretBase
    {
        public Collider GunCollider;
        public TurretGun Gun;

        protected override bool IsCorrectTargetType(GameObject targetObject)
        {
            return targetObject.tag == GameEntityType.Player.ToString();
        }

        protected override void Attack()
        {
            GunCollider.transform.LookAt(Target.transform);
            Gun.FireAtTarget(Target);
        }
    }
}
