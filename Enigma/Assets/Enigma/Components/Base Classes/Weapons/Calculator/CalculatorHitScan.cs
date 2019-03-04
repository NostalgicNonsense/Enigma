using Enigma.Components.Base_Classes.TeamSettings.Enums;
using Enigma.Components.Base_Classes.Vehicle.ComponentScripts;
using Enigma.Enums;
using UFPS.Base.Scripts.Gameplay.Combat;
using UnityEngine;

namespace Enigma.Components.Base_Classes.Weapons.Calculator
{
    public class CalculatorHitScan : vp_Bullet
    {
        protected override void TryDamage()
        {
            if (m_Hit.collider.tag == GameEntityType.Structure.ToString() || m_Hit.collider.tag == GameEntityType.Vehicle.ToString())
            {
                HealOrDamageVehiclesOrBuildings();
            }
            else if(m_Hit.collider.tag == GameEntityType.Player.ToString())
            {
                HealOrDamagePlayers();   
            }
        }

        private void HealOrDamagePlayers()
        {
            if (GetAndCheckIfHitIsSameTeam())
            {
                var damageHandler = m_Hit.collider.gameObject.GetComponent<vp_DamageHandler>();
                damageHandler.Damage(Damage * -1); // lol
            }
            else
            {
                var damageHandler = m_Hit.collider.gameObject.GetComponent<vp_DamageHandler>();
                damageHandler.Damage(Damage);
            }
        }

        private void HealOrDamageVehiclesOrBuildings()
        {
            if (GetAndCheckIfHitIsSameTeam())
            {
                var damageHandler = m_Hit.collider.gameObject.GetComponent<EnigmaDamageHandler>();
                damageHandler.Heal(Damage);
            }
            else
            {
                var damageHandler = m_Hit.collider.gameObject.GetComponent<EnigmaDamageHandler>();
                damageHandler.TakeDamage(Damage);
            }
        }

        private bool GetAndCheckIfHitIsSameTeam()
        {
            var team = GetComponentInParent<Team>();
            var hitTeam = m_Hit.collider.gameObject.GetComponent<Team>();
            Debug.Assert(hitTeam != null);
            Debug.Assert(team != null);
            return team.SameTeam(hitTeam);
        }
    }
}
