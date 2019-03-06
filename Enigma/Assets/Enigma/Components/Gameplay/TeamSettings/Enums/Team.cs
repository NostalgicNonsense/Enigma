﻿using UnityEngine;

namespace Enigma.Components.Gameplay.TeamSettings.Enums
{
    public class Team : MonoBehaviour
    {
        public TeamName TeamName;

        public Color32 GetTeamColor()
        {
            switch (TeamName)
            {
                case TeamName.Neutral:
                    return new Color32(100, 100, 100, 255);
                case TeamName.Critters:
                    return new Color32(255, 255, 0, 255);
                case TeamName.Team1:
                    return new Color32(255, 100, 0, 255);
                case TeamName.Team2:
                    return new Color32(0, 100, 255, 255);
                default:
                    return new Color32(50, 50, 50, 255);
            }
        }

        public bool SameTeam(Team otherTeam)
        {
            return TeamName == otherTeam.TeamName;
        }
    }


    public enum TeamName
    {
        Neutral,
        Critters,
        Team1,
        Team2,
    }
}
