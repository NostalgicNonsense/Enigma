using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.UI.Minimap
{
    public class Minimap : MonoBehaviour
    {
        public Transform targetFollow;
        public float ZoomOut = 200;
        public bool rotateWithTarget;

        private RectTransform MinimapUI;
        private Camera cameraMinimap;
        private bool isMaximized = false;

        public float MinimizedScale = 1f;
        public float MaximizedScale = 4f;

        void Start()
        {
            cameraMinimap = GetComponent<Camera>();
            MinimapUI = GetComponentInParent<RectTransform>();

            if (rotateWithTarget)
            {
                cameraMinimap.transform.SetParent(targetFollow);
            }
        }

        void Update()
        {
            CheckForInputs();
            if (rotateWithTarget == false)
            {
                cameraMinimap.transform.position = new Vector3(targetFollow.position.x, ZoomOut, targetFollow.position.z);
                cameraMinimap.transform.eulerAngles = new Vector3(90, 0, 0);
            }
        }

        private void CheckForInputs()
        {
            if (Input.GetButtonDown("General_MapToggle"))
            {
                if (isMaximized)
                {
                    Minimize();
                }
                else
                {
                    Maximize();
                }
            }
        }

        private void Minimize()
        {
            MinimapUI.localScale = new Vector3(MinimizedScale, MinimizedScale, 1);
            isMaximized = false;
        }

        private void Maximize()
        {
            MinimapUI.localScale = new Vector3(MaximizedScale, MaximizedScale, 1);
            isMaximized = true;
        }
    }
}
