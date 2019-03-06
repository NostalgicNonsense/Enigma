using Enigma.Components.Gameplay.Buildings.Turrets.Targeting;
using Enigma.Components.Gameplay.Buildings.Turrets.TurretWeapons;
using UnityEngine;

namespace Enigma.Components.Gameplay.Buildings.Turrets
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