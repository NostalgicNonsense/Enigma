using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using UnityEngine;

namespace Assets.Enigma.Components.UI.Minimap
{
    public class MinimapIcon : Photon.MonoBehaviour
    {
        private Team team;

        void Start()
        {
            team = GetComponentInParent<Team>();
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = team.GetTeamColor();
        }
    }
}
