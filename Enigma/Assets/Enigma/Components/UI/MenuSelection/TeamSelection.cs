using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using Assets.Enigma.Components.Network;
using UnityEngine;

namespace Assets.Enigma.Components.UI.MenuSelection
{
    public class TeamSelection : MonoBehaviour
    {
        private CanvasRenderer uiBackground;
        private NetworkManagerExtension netWorkManagerExtension;

        public void Start()
        {
            netWorkManagerExtension = GameObject.FindObjectOfType<NetworkManagerExtension>();
            uiBackground = GetComponentInChildren<CanvasRenderer>();
            Debug.Log("UI Background: " + uiBackground.name);

            HideMenu();
        }

        public void Update()
        {
            if (Input.GetButtonDown("General_TeamSelection"))
            {
                ToggleMenu();
            }
        }

        public void SelectTeam1()
        {
            TeamSelected(TeamName.Team1);
        }

        public void SelectTeam2()
        {
            TeamSelected(TeamName.Team2);
        }

        private void TeamSelected(TeamName teamName)
        {
            HideMenu();

            netWorkManagerExtension.SpawnPlayer(teamName);
        }

        public void ShowMenu()
        {
            uiBackground.gameObject.SetActive(true);
        }

        public void HideMenu()
        {
            uiBackground.gameObject.SetActive(false);
        }

        private void ToggleMenu()
        {
            if (uiBackground.gameObject.activeSelf == false)
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