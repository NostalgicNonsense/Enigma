using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Captureables
{
    public class CaptureFlag : MonoBehaviour
    {
        public Material Neutral;
        public Material Team1;
        public Material Team2;

        public MeshRenderer[] flagRenders;

        void Start()
        {
            Team team = GetComponentInParent<Team>();
            SetTeam(team.TeamName);
        }

        public void SetTeam(TeamName teamName)
        {
            if (teamName == TeamName.Team1)
            {
                foreach (var rend in flagRenders)
                {
                    rend.material = Team1;
                }
            }
            else if (teamName == TeamName.Team2)
            {
                foreach (var rend in flagRenders)
                {
                    rend.material = Team2;
                }
            }
        }
    }
}
