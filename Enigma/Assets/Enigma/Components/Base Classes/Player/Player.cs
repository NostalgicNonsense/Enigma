using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Enigma.Components.Base_Classes.Player
{
    public class Player : MonoBehaviour, IPlayer
    {
        Team team;
        void Start()
        {
            team = GetComponent<Team>();
        }

        void Update()
        {
            if (Input.GetButtonDown("General_Respawn"))
            {
            }
        }
    }
}
