using Enigma.Components.Base_Classes.TeamSettings.Enums;
using UnityEngine;

namespace Enigma.Components.UI.Minimap
{
    public class MinimapIcon : MonoBehaviour
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
