using Assets.Enigma.Components.Base_Classes.Maps.Preplaced;
using Assets.Enigma.Components.Base_Classes.Player;
using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Enigma.Components.Network
{
    public class NetworkManagerExtension : NetworkManager
    {
        public GameObject PlayerPrefab;
        public List<MultiplayerSpawnType> ListPrePlaced;
        public override void OnServerConnect(NetworkConnection conn)
        {
            Debug.Log("OnServerConnect");
            var test = conn.connectionId;

            foreach (MultiplayerSpawnType spawnType in ListPrePlaced)
            {
                spawnType.SpawnObject();
            }
        }

        public void SpawnPlayer(TeamName teamName)
        {
            CreatePlayer(teamName);
            KillSpectatorPlayer();
        }

        /// <summary>
        /// Temporary function until we only have a better spectator and player system.
        /// </summary>
        private void KillSpectatorPlayer()
        {
            var specator = NetworkManager.FindObjectsOfType<PlayerSpectator>().First();
            if (specator != null)
            {
                Destroy(specator.gameObject);
            }
            else
            {
                Debug.Log("No spectator players for KillSpectatorPlayer function");
            }
        }

        private void CreatePlayer(TeamName teamName)
        {
            var spawnPoints = NetworkManager.FindObjectsOfType<SpawnPos>();
            foreach (var spawn in spawnPoints)
            {
                var teamSpawn = spawn.GetTeam();
                if (teamSpawn.TeamName == teamName)
                {
                    var spawnObj = (GameObject)Instantiate(PlayerPrefab, spawn.transform.position, spawn.transform.rotation);
                    spawnObj.GetComponent<Team>().TeamName = teamSpawn.TeamName;
                    NetworkServer.Spawn(spawnObj);
                    break;
                }
            }
        }
    }
}
