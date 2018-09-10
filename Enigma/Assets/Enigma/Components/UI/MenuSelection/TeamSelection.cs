using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using Assets.Enigma.Components.Network;
using UnityEngine;

namespace Assets.Enigma.Components.UI.MenuSelection
{
    public class TeamSelection : UISelection
    {
        private NetworkManagerExtension netWorkManagerExtension;

        private vp_MPMaster vp_master;

        public void Start()
        {
            netWorkManagerExtension = GameObject.FindObjectOfType<NetworkManagerExtension>();
            
            vp_master = FindObjectOfType<vp_MPMaster>();

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

            var playerTypeName = vp_MPTeamManager.Instance.GetTeamPlayerTypeName(teamNumber);
            var placement = vp_MPPlayerSpawner.GetRandomPlacement(vp_MPTeamManager.GetTeamName(teamNumber));

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