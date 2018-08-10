using Assets.Enigma.Components.Base_Classes.Buildings.Turrets.Targeting;
using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Turrets
{
    public abstract class TurretBase : MonoBehaviour
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

        protected abstract void Attack(GameObject target);
    }
}