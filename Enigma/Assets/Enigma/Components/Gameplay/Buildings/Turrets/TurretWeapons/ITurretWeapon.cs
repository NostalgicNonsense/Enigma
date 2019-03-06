using UnityEngine;

namespace Enigma.Components.Gameplay.Buildings.Turrets.TurretWeapons
{
    public interface ITurretWeapon
    {
        void Attack(GameObject target);
    }
}
