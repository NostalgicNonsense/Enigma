using UnityEngine;

namespace Marketplace.SampleScenes.Menu.Scripts
{
    public class MenuSceneLoader : MonoBehaviour
    {
        public GameObject menuUI;

        private GameObject m_Go;

        void Awake ()
        {
            if (m_Go == null)
            {
                m_Go = Instantiate(menuUI);
            }
        }
    }
}
