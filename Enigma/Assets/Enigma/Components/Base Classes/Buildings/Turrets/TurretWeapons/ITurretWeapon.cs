using UnityEngine;

namespace Enigma.Components.Base_Classes.Buildings.Turrets.TurretWeapons
{
    public interface ITurretWeapon
    {
        void Attack(GameObject target);
    }
}
