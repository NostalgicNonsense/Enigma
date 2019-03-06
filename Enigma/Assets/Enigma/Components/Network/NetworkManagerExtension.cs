//using System.Linq;
//using Enigma.Components.Base_Classes.Player;
//using Enigma.Components.Base_Classes.TeamSettings.Enums;
//using UnityEngine;

//namespace Enigma.Components.Network
//{
//    public class NetworkManagerExtension : NetworkManager
//    {
//        public GameObject SpectatorPrefab;
//        public GameObject SpectatorSpawn;
//        public override void OnServerConnect(NetworkConnection conn)
//        {
//            Debug.Log("OnServerConnect");
//            SpawnSpectator();
//        }

//        public void SpawnPlayer(TeamName teamName)
//        {
//            CreatePlayer(teamName);
//            KillSpectatorPlayer();
//        }

//        private void SpawnSpectator()
//        {
//            var spectatorObj = Instantiate(SpectatorPrefab, SpectatorSpawn.transform.position, SpectatorPrefab.transform.rotation);
//            NetworkServer.Spawn(spectatorObj);
//        }

//        /// <summary>
//        /// Temporary function until we only have a better spectator and player system.
//        /// </summary>
//        private static void KillSpectatorPlayer()
//        {
//            var specator = FindObjectsOfType<PlayerSpectator>().First();
//            if (specator != null)
//            {
//                Destroy(specator.gameObject);
//            }
//            else
//            {
//                Debug.Log("No spectator players for KillSpectatorPlayer function");
//            }
//        }

//        private void CreatePlayer(TeamName teamName)
//        {
//            var spawnPoints = FindObjectsOfType<SpawnPos>();
//            Debug.Log("# of Spawn Points found: " + spawnPoints.Length + " for team: " + teamName);
//            foreach (var spawn in spawnPoints)
//            {
//                var teamSpawn = spawn.GetTeam();
//                if (teamSpawn.TeamName == teamName)
//                {
//                    var spawnObj = Instantiate(playerPrefab, spawn.transform.position, spawn.transform.rotation);
//                    spawnObj.GetComponent<Team>().TeamName = teamSpawn.TeamName;
//                    NetworkServer.Spawn(spawnObj);
//                    break;
//                }
//            }
//        }
//    }
//}