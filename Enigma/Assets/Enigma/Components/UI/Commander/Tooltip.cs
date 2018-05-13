using Assets.Enigma.Components.Base_Classes.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Enigma.Components.UI.Commander
{
    public class Tooltip : MonoBehaviour
    {
        public Text Name;
        public Text Hotkey;
        public Text Description;
        
        public Text CostMoney;
        public Text CostOil;

        public Image IconMoney;
        public Image IconOil;

        public void ShowTooltip(BuildingStats stats)
        {
            Name.text = stats.Name;
            Hotkey.text = "[ " + stats.Hotkey + " ]";
            Description.text = stats.Description;
            CostMoney.text = stats.costMoney.ToString();
            CostOil.text = stats.costOil.ToString();
        }
    }
}
