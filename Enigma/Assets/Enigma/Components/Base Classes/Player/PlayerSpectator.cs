using Assets.Enigma.Components.UI.MenuSelection;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Player
{
    public class PlayerSpectator : MonoBehaviour
    {
        private bool isInit = false;
        private TeamSelection teamSelection;

        public void Start()
        {
            
        }

        void Update()
        {
            if (isInit == false)
            {
                teamSelection = GameObject.FindObjectOfType<TeamSelection>();
                isInit = true;
                if (teamSelection != null)
                {
                    teamSelection.ShowMenu();
                }
            }
        }
    }
}
