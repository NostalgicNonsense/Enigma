//using Enigma.Components.UI.MenuSelection;
//using UnityEngine;

//namespace Enigma.Components.Base_Classes.Player
//{
//    public class PlayerSpectator : MonoBehaviour
//    {
//        private bool isInit = false;
//        private TeamSelection _teamSelection;

//        public void Start()
//        {
//            _teamSelection = FindObjectOfType<TeamSelection>();
//        }

//        public void Update()
//        {
//            if (isInit == false)
//            {
//                _teamSelection = FindObjectOfType<TeamSelection>();
//                isInit = true;
//                if (_teamSelection != null)
//                {
//                    _teamSelection.ShowMenu();
//                }
//            }
//            else if (Input.GetKeyDown("1"))
//            {
//                _teamSelection.SelectTeam1();
//            }
//            else if (Input.GetKeyDown("2"))
//            {
//                _teamSelection.SelectTeam2();
//            }
//        }
//    }
//}
