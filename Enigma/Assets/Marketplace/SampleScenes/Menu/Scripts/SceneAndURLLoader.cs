using UnityEngine;
using UnityEngine.SceneManagement;

namespace Marketplace.SampleScenes.Menu.Scripts
{
    public class SceneAndURLLoader : MonoBehaviour
    {
        private PauseMenu m_PauseMenu;


        private void Awake ()
        {
            m_PauseMenu = GetComponentInChildren <PauseMenu> ();
        }


        public void SceneLoad(string sceneName)
        {
            //PauseMenu pauseMenu = (PauseMenu)FindObjectOfType(typeof(PauseMenu));
            m_PauseMenu.MenuOff ();
            SceneManager.LoadScene(sceneName);
        }


        public void LoadURL(string url)
        {
            Application.OpenURL(url);
        }
    }
}

