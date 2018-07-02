using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.UI.MenuSelection
{
    public class TeamSelection : MonoBehaviour
    {
        public GameObject UIObject;

        void Update()
        {
            if (Input.GetButtonDown("General_TeamSelection"))
            {
                ToggleMenu();
            }
        }

        public void SelectTeam1()
        {
            TeamSelected(TeamName.Team_1_People);
        }

        public void SelectTeam2()
        {
            TeamSelected(TeamName.Team_2_TheOrder);
        }

        private void TeamSelected(TeamName teamName)
        {
            HideMenu();
        }

        private void ShowMenu()
        {
            UIObject.SetActive(true);
        }

        private void HideMenu()
        {
            UIObject.SetActive(false);
        }

        private void ToggleMenu()
        {
            if (UIObject.activeSelf == false)
            {
                ShowMenu();
            }
            else
            {
                HideMenu();
            }
        }
    }
}
