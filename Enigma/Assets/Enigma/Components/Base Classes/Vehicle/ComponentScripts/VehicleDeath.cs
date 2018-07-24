using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts
{
    public class VehicleDeath : MonoBehaviour
    {
        public GameObject SpawnOnDeath;

        public void Die()
        {
            Instantiate(SpawnOnDeath);
        }
    }
}
