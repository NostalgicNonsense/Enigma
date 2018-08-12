using Assets.Enigma.Components.Base_Classes.Buildings.Turrets.Targeting;
using Assets.Enigma.Components.Base_Classes.Buildings.Turrets.TurretWeapons;
using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Turrets
{
    public class Turret : MonoBehaviour
    {
        public Team TeamOfTurret;

        public void FixedUpdate()
        {
            var target = GetComponent<ITargeter>().Target;
            if (target != null)
            {
                Attack(target);
            }
        }

        private void Attack(GameObject target)
        {
            GetComponent<ITurretWeapon>().Attack(target);
        }
    }
}