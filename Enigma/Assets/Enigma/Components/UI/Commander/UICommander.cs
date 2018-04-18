using Assets.Enigma.Components.UI.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Enigma.Components.Base_Classes.Commander;

namespace Assets.Enigma.Components.UI
{
    public class UICommander : MonoBehaviour
    {
        public BuildingPlacement BuildingPlacement { get; private set; }

        void Start()
        {
            BuildingPlacement = GetComponentInParent<BuildingPlacement>();
        }

        void Update()
        {

        }

        private void CheckRightClick()
        {

        }
    }
}
