using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using UnityEngine;

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
