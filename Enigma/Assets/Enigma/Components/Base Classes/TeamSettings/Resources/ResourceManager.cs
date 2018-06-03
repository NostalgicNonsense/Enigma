using Assets.HelpClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.TeamSettings.Resources
{
    public class ResourceManager : MonoBehaviour
    {
        public Money Money { get; private set; }
        public Oil Oil { get; private set; }

        void Start()
        {
            Money = new Money();
            Oil = new Oil();

            Money.Add(653);
            Oil.Add(55);
        }

        public Tuple<Money, Oil> GetResources()
        {
            return new Tuple<Money, Oil>(Money, Oil);
        }
    }
}
