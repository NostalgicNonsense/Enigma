using Enigma.Components.Base_Classes.TeamSettings.Enums;
using UnityEngine;

namespace Enigma.Components.UI.MenuSelection
{
    public class TeamSelection : UISelection
    {

        public void Start()
        {
            
            Init();
        }

        public void Update()
        {
            if (Input.GetButtonDown("General_TeamSelection"))
            {
                ToggleMenu();
            }

            if (IsVisible)
            {
                if (Input.GetKeyDown("1"))
                {
                    SelectTeam1();
                }
                else if (Input.GetKeyDown("2"))
                {
                    SelectTeam2();
                }
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
            int teamNumber = GetTeamNumber(teamName);

            //vp_master.photonView.RPC("ReceiveInitialSpawnInfo", PhotonTargets.All, PhotonNetwork.player.ID, PhotonNetwork.player, placement.Position, placement.Rotation, playerTypeName, teamNumber);


            //netWorkManagerExtension.SpawnPlayer(teamName);
        }

        private int GetTeamNumber(TeamName teamName) {
            switch (teamName)
            {
                case TeamName.Team1:
                    return 0;
                case TeamName.Team2:
                    return 1;
                default:
                    return -1;
            }
        }
    }
}