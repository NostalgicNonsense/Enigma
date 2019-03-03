/////////////////////////////////////////////////////////////////////////////////
//
//	vp_PulsingLight.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a simple script for making a light flash in a sinus motion
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

namespace UFPS.Base.Scripts.Effects
{
    public class vp_PulsingLight : MonoBehaviour
    {

        Light m_Light = null;

        public float m_MinIntensity = 2.0f;
        public float m_MaxIntensity = 5.0f;
        public float m_Rate = 1.0f;


        /// <summary>
        /// Caches the light.
        /// </summary>
        void Start ()
        {
            m_Light = GetComponent<Light>();
        }


        /// <summary>
        /// Flashes the light up and down by applying a sine wave to its intensity.
        /// </summary>
        void Update ()
        {

            if (m_Light == null)
                return;

            m_Light.intensity = m_MinIntensity + Mathf.Abs(Mathf.Cos((Time.time * m_Rate)) * (m_MaxIntensity - m_MinIntensity));

        }

    }
}
