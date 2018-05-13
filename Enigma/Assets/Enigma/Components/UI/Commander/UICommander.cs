using Assets.Enigma.Components.UI.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Enigma.Components.Base_Classes.Commander;
using Assets.Enigma.Components.UI.Commander;

namespace Assets.Enigma.Components.UI
{
    public class UICommander : MonoBehaviour
    {
        public Tooltip Tooltip { get; private set; }
        public BuildingPlacement BuildingPlacement { get; private set; }

        void Start()
        {
            BuildingPlacement = GetComponentInParent<BuildingPlacement>();
            Tooltip = GetComponentInParent<Tooltip>();
        }

        void Update()
        {

        }

        private void CheckRightClick()
        {

        }
    }
}
