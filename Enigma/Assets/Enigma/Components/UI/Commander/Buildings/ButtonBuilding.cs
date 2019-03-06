using Enigma.Components.Gameplay.Buildings;
using Enigma.Components.Gameplay.TeamSettings.Enums;
using Enigma.Components.Gameplay.TeamSettings.Resources;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Enigma.Components.UI.Commander.Buildings
{
    public class ButtonBuilding : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public BuildingHologram buildingHologram;
        protected UICommander uICommander;
        private Image buttonImage;

        private BuildingStats stats;

        private static Color colorAfford = Color.white;
        private static Color colorTooLow = Color.red;

        private Color colorTextMoney;
        private Color colorTextOil;

        private bool isHighLighted = false;

        private AudioSource soundCantAfford;
        private Team team;


        // Use this for initialization
        void Start()
        {
            buttonImage = GetComponent<Button>().GetComponent<Image>();
            uICommander = GetComponentInParent<UICommander>();
            soundCantAfford = GetComponentInParent<AudioSource>();
            team = GetComponentInParent<Team>();
            stats = GetComponentInChildren<BuildingStats>();
        }

        // Update is called once per frame
        void Update()
        {
            if (isHighLighted)
            {
                CheckResources();
            }
        }

        public void Click()
        {
            if (colorTextMoney == colorAfford && colorTextOil == colorAfford)
            {
                uICommander.BuildingPlacement.SetSelectedHologram(buildingHologram);
            }
            else
            {
                if (soundCantAfford != null && soundCantAfford.isPlaying == false)
                {
                    soundCantAfford.Play();
                }
            }
        }
    
        public void OnPointerEnter(PointerEventData eventData)
        {
            buttonImage.color = Color.gray;
            isHighLighted = true;
            uICommander.Tooltip.ShowTooltip(stats, colorTextMoney, colorTextOil);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            buttonImage.color = Color.white;
            isHighLighted = false;
        }

        private void CheckResources()
        {
            var resources = uICommander.ResourceManager.GetResources(team.TeamName);
            CheckMoney(resources.First);
            CheckOil(resources.Second);
            uICommander.Tooltip.UpdateColors(colorTextMoney, colorTextOil);
        }

        private void CheckMoney(Money current)
        {
            if (stats.costMoney <= current.Current)
            {
               colorTextMoney = colorAfford;
            }
            else
            {
                colorTextMoney = colorTooLow;
            }
        }

        private void CheckOil(Oil current)
        {
            if (stats.costOil <= current.Current)
            {
                colorTextOil = colorAfford;
            }
            else
            {
                colorTextOil = colorTooLow;
            }
        }
    }
}