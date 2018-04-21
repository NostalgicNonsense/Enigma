using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Enigma.Components.UI;
using Assets.Enigma.Components.Base_Classes.Buildings;

namespace Assets.Enigma.Components.UI.Buildings
{
    public class ButtonBuilding : MonoBehaviour
    {
        public BuildingHologram buildingHologram;
        protected UICommander uICommander;
        // Use this for initialization
        void Start()
        {
            uICommander = GetComponentInParent<UICommander>();
            Init();
        }

        protected virtual void Init()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Click()
        {
            uICommander.BuildingPlacement.SetSelectedHologram(buildingHologram);
        }
    }
}