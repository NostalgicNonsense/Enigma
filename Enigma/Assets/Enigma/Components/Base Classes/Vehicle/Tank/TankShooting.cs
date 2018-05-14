using UnityEngine;
using UnityEngine.UI;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.Tank
{
    public class TankShooting : MonoBehaviour
    {
        public int m_PlayerNumber = 1;            // Used to identify the different players.
        public Transform m_FireTransform;           // A child of the tank where the shells are spawned.

        private string m_FireButton;                // The input axis that is used for launching shells.
        public Color colorShell;

        public Weapon WeaponEquipped;

        private void OnEnable()
        {
            
        }

        public void SetWeapon(Weapon weaponNew)
        {
            WeaponEquipped = weaponNew;
            weaponNew.Refresh();
        }

        private void Start ()
        {
            // The fire axis is based on the player number.
            m_FireButton = "Fire" + m_PlayerNumber;

            // The rate that the launch force charges up is the range of possible forces by the max charge time.

        }

        private void Update ()
        {
            // The slider should have a default value of the minimum launch force.

            // ... launch the shell.
            if (WeaponEquipped != null)
            {
                if (WeaponEquipped.firingCooldown <= 0 && Input.GetButton(m_FireButton))
                {
                    WeaponEquipped.Fire(m_FireTransform, colorShell, m_PlayerNumber);
                }
            }
        }
    }
}