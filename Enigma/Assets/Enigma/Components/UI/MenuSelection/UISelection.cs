using System;
using UnityEngine;

namespace Enigma.Components.UI.MenuSelection
{
    public class UISelection : MonoBehaviour
    {
        private CanvasRenderer uiBackground;

        public Boolean IsVisible { get; private set; }

        protected void Init()
        {
            uiBackground = GetComponentInChildren<CanvasRenderer>();
            Debug.Log("UI Background: " + uiBackground.name);
            HideMenu();
        }

        public virtual void ShowMenu()
        {
            uiBackground.gameObject.SetActive(true);
            IsVisible = true;
            //Cursor.lockState = CursorLockMode.None;
        }

        public virtual void HideMenu()
        {
            uiBackground.gameObject.SetActive(false);
            IsVisible = false;
            //Cursor.lockState = CursorLockMode.Locked;
        }

        protected virtual void ToggleMenu()
        {
            if (uiBackground.gameObject.activeSelf == false)
            {
                ShowMenu();
            }
            else
            {
                HideMenu();
            }
        }
    }
}
