using UnityEngine;

namespace Photon_Unity_Networking.UtilityScripts
{
    public class QuitOnEscapeOrBack : MonoBehaviour
    {
        private void Update()
        {
            // "back" button of phone equals "Escape". quit app if that's pressed
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
    }
}
