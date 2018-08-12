using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Turrets.TurretWeapons
{
    public interface ITurretWeapon
    {
        void Attack(GameObject target);
    }
}
