using Enigma.Components.Base_Classes.Buildings.Turrets.Targeting;
using Enigma.Components.Base_Classes.Buildings.Turrets.TurretWeapons;
using UnityEngine;

namespace Enigma.Components.Base_Classes.Buildings.Turrets
{
    public class Turret : MonoBehaviour
    {
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