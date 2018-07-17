using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using Assets.HelpClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.TeamSettings.Resources
{
    public class ResourceTeam : MonoBehaviour
    {
        public Money Money { get; private set; }
        public Oil Oil { get; private set; }

        public TeamName team;

        void Start()
        {
            Money = new Money();
            Oil = new Oil();

            team = GetComponent<TeamName>();

            Money.Add(653);
            Oil.Add(55);
        }

        public void Add(int money, int oil)
        {
            Money.Add(money);
            Oil.Add(oil);
        }

        public Tuple<Money, Oil> GetResources()
        {
            return new Tuple<Money, Oil>(Money, Oil);
        }
    }
}
