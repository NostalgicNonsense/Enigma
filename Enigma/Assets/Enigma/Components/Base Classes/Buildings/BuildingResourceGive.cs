using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using Assets.Enigma.Components.Base_Classes.TeamSettings.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings
{
    public class BuildingResourceGive : MonoBehaviour
    {
        [SerializeField]
        private int creditsToGive = 1;
        [SerializeField]
        private int oilToGive = 1;

        [SerializeField]
        private float secondsPerGive = 1;

        private Team team;

        private ResourceTeam resourceTeam;

        void Start()
        {
            cooldown = secondsPerGive;
            team = GetComponent<Team>();
            resourceTeam = GetComponent<ResourceTeam>();
        }

        private float cooldown = 0;
        void FixedUpdate()
        {
            //Todo: Move this to multiplayer
            cooldown -= Time.deltaTime;
            if (cooldown <= 0)
            {
                cooldown = secondsPerGive;
                GiveResources();
            }
        }

        public void SetTeam(TeamName teamName)
        {
            team.TeamName = teamName;
        }

        private void GiveResources()
        {
            if (resourceTeam != null && (resourceTeam.team == TeamSettings.Enums.TeamName.Team_1_People || resourceTeam.team == TeamSettings.Enums.TeamName.Team_2_TheOrder))
            {
                resourceTeam.Add(creditsToGive, oilToGive);
            }
        }
    }
}
