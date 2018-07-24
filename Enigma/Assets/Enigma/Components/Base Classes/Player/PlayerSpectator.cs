using Assets.Enigma.Components.UI.MenuSelection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Player
{
    class PlayerSpectator : MonoBehaviour
    {
        public TeamSelection TeamSelection;
        void Start()
        {
            if (TeamSelection != null)
            {
                TeamSelection.ShowMenu();
            }
        }
    }
}
