using Enigma.Components.Base_Classes.TeamSettings.Enums;
using Enigma.Components.Base_Classes.TeamSettings.Resources;
using UnityEngine;

namespace Enigma.Components.Base_Classes.Buildings
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

        private ResourceTeams resourceTeam;

        void Start()
        {
            cooldown = secondsPerGive;
            team = GetComponent<Team>();
            resourceTeam = GameObject.FindObjectOfType<ResourceTeams>();
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
            if (resourceTeam != null)
            {
                resourceTeam.Add(creditsToGive, oilToGive, team.TeamName);
            }
        }
    }
}
