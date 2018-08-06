using Assets.Enigma.Components.UI.MenuSelection;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Player
{
    class PlayerSpectator : MonoBehaviour
    {
        private bool isInit = false;
        public TeamSelection TeamSelection;

        public void Start()
        {
            if (isInit == false)
            {
                isInit = true;
                if (TeamSelection != null)
                {
                    TeamSelection.ShowMenu();
                }
            }
        }

        void Update()
        {

        }
    }
}
