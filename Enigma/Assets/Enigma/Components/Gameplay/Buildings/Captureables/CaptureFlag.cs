﻿using Enigma.Components.Gameplay.TeamSettings.Enums;
using UnityEngine;

namespace Enigma.Components.Gameplay.Buildings.Captureables
{
    public class CaptureFlag : MonoBehaviour
    {
        public Material Neutral;
        public Material Team1;
        public Material Team2;

        public MeshRenderer[] flagRenders;

        public void Start()
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
