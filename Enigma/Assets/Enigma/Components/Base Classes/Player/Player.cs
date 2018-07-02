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
            Respawn();
        }

        void Update()
        {
            if (Input.GetButtonDown("General_MapToggle"))
            {
                Respawn();
            }
        }

        public void Respawn()
        {
            Debug.Log("Respawning player");
            var spawnPoints = NetworkManager.FindObjectsOfType<NetworkStartPosition>();
            foreach(var spawn in spawnPoints)
            {
                var teamSpawn = spawn.GetComponentInParent<Team>();
                if (teamSpawn.TeamName == team.TeamName)
                {
                    SpawnPlayerAtPosition(spawn);
                    break;
                }
            }
        }

        private void SpawnPlayerAtPosition(NetworkStartPosition pos)
        {
            transform.position = pos.transform.position;
        }
    }
}
