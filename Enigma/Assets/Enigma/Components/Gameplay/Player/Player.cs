using Enigma.Components.Gameplay.TeamSettings.Enums;
using UnityEngine;

namespace Enigma.Components.Gameplay.Player
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
