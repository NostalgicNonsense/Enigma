namespace Assets.Enigma.Components.Base_Classes.Weapons.Calculator
{
    public class CalculatorReloader : vp_WeaponReloader
    {
        protected override void Start()
        {
            InvokeRepeating("Reload", 0, 2.0f);
        }

        private void Reload()
        {
            if (m_Player.CurrentWeaponAmmoCount.Get() < m_Player.CurrentWeaponMaxAmmoCount.Get())
            {
                m_Player.CurrentWeaponAmmoCount.Set(m_Player.CurrentWeaponAmmoCount.Get() + 1);
            }
        }
    }
}
