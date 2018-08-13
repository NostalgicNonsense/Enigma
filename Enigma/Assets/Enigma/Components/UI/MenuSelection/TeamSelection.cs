using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using Assets.Enigma.Components.Network;
using UnityEngine;

namespace Assets.Enigma.Components.UI.MenuSelection
{
    public class TeamSelection : MonoBehaviour
    {

        public GameObject UIObject;
        private NetworkManagerExtension netWorkManagerExtension;


        public void Start()
        {

            netWorkManagerExtension = GameObject.FindObjectOfType<NetworkManagerExtension>();

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
            UiObject.SetActive(true);
        }

        public void HideMenu()
        {
            UiObject.SetActive(false);
        }

        private void ToggleMenu()
        {
            if (UiObject.activeSelf == false)
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