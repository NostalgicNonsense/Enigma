using Assets.Enigma.Components.Base_Classes.Shells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.Tank
{
    public class Weapon : MonoBehaviour
    {
        public Rigidbody m_Shell;                   // Prefab of the shell.

        private AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.

        private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        private float firingDelay;

        public float firingDelayDefault = 1f;
        public float firingCooldown { private set; get; }
        public float m_MinLaunchForce = 15f;        // The force given to the shell if the fire button is not held.

        private int ammoCurrent;
        public int ammoMax = 10;

        private int kills = 0;
       
        public void Refresh()
        {
            firingCooldown = 0;
            firingDelay = firingDelayDefault;
            ammoCurrent = ammoMax;
        }

        public void RoadKill(int killsTotal)
        {
            kills = killsTotal;
            firingDelay = firingDelayDefault - GetCooldownReductionPerKill();
            Debug.Log("FiringDelay: " + firingDelay);
        }
        
        private float GetCooldownReductionPerKill()
        {
            return (kills + 1) / 250;
        }

        private float GetDamageIncrease(float damage)
        {
            return damage + (damage * ((kills + 1) / 25));
        }

        private float GetShellSpeed()
        {
            //15f
            return m_CurrentLaunchForce + (m_CurrentLaunchForce * ((kills + 1) / 25)) ;
        }

        public void SetEmpty()
        {
            ammoCurrent = 0;
        }


        void Start()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_ShootingAudio = GetComponent<AudioSource>();
        }

        void Update()
        {
            if (firingCooldown > 0)
            {
                firingCooldown -= Time.deltaTime;
            }
        }

        public void Fire(Transform weaponTransform, Color colorShell, int playerNumber)
        {
            if (ammoCurrent > 0)
            {
                firingCooldown = firingDelay;
                ammoCurrent--;

                Rigidbody shellInstance =
                    Instantiate(m_Shell, weaponTransform.position, weaponTransform.rotation) as Rigidbody;
                ShellExplosion shellExplosion = shellInstance.GetComponent<ShellExplosion>();
                if (shellExplosion != null)
                {
                    shellExplosion.m_MaxDamage = GetDamageIncrease(shellExplosion.m_MaxDamageDefault);
                }

                var shellColor = shellInstance.GetComponent<Light>();
                shellColor.color = colorShell;

                // Set the shell's velocity to the launch force in the fire position's forward direction.
                shellInstance.velocity = GetShellSpeed() * weaponTransform.forward;

                // Change the clip to the firing clip and play it.
                m_ShootingAudio.Play();

                // Reset the launch force.  This is a precaution in case of missing button events.
                m_CurrentLaunchForce = m_MinLaunchForce;
            }
        }
    }
}
