using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.TeamSettings.Enums
{
    public class Team : MonoBehaviour
    {
        public TeamName TeamName;
    }

    public enum TeamName
    {
        Neutral,
        Critters,
        Team_1_People,
        Team_2_TheOrder,
    }
}
