﻿using System;
using UnityEditor;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts
{
    public class EnigmaDamageHandler : MonoBehaviour
    {
        public float Health;
        public float Max;

        public void TakeDamage(float damageToTake)
        {
            Health = -damageToTake;
            if (Health < 0)
            {
                //How do we want to handle dying?
                var vehicle = GetComponent<SimpleCarController>(); // well this will need to be fixed via abstraction.
                vehicle.EjectPassangers();
                //Destroy(ParentVehicle);
                Destroy(this);
            }
        }

        public void Heal(float healthToHeal)
        {
            var healthAfterHealing = healthToHeal + Health;
            Health = Math.Max(Max, healthAfterHealing);
        }
    }
}