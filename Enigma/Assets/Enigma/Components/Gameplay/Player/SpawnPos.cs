﻿using Enigma.Components.Gameplay.TeamSettings.Enums;
using UnityEngine;

namespace Enigma.Components.Gameplay.Player
{
    public class SpawnPos : MonoBehaviour
    {
        private Team teamParent;
        public Team GetTeam()
        {
            if (teamParent == null)
            {
                teamParent = GetComponentInParent<Team>();
            }
            return teamParent;
        }

        public TeamName GetTeamName()
        {
            if (teamParent == null)
            {
                teamParent = GetComponentInParent<Team>();
            }
            return teamParent.TeamName;
        }
        void Start()
        {
            MeshRenderer rend = GetComponent<MeshRenderer>();
            rend.enabled = false;
        }
    }
}
