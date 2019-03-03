using Enigma.Components.Base_Classes.TeamSettings.Enums;
using UnityEngine;

namespace Enigma.Components.Base_Classes.Player
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
