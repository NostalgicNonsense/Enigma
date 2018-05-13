using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Enigma.Components.UI;
using Assets.Enigma.Components.Base_Classes.Buildings;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Enigma.Components.UI.Commander;

namespace Assets.Enigma.Components.UI.Buildings
{
    public class ButtonBuilding : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public BuildingHologram buildingHologram;
        protected UICommander uICommander;
        private Image buttonImage;

        protected BuildingStats buildingStats;


        // Use this for initialization
        void Start()
        {
            buttonImage = GetComponent<Button>().GetComponent<Image>();
            uICommander = GetComponentInParent<UICommander>();
            Init();
        }

        /// <summary>
        /// To be called from the children who inheritance from this class.
        /// </summary>
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

        protected virtual void ShowTooltip()
        {

        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            buttonImage.color = Color.gray;
            uICommander.Tooltip.ShowTooltip(buildingStats);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            buttonImage.color = Color.white;
        }
    }
}