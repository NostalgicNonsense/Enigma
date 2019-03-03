using UnityEngine;

namespace Enigma.Components.UI.Minimap
{
    public class Minimap : MonoBehaviour
    {
        public Transform targetFollow;
        public float ZoomOut = 200;
        public float ZoomLowest = 5;
        public float ZoomHighest = 500;
        public float ZoomSpeed = 50f;
        public bool rotateWithTarget;

        public RectTransform MinimapUI;
        private Camera cameraMinimap;
        private bool isMaximized = false;

        public float MinimizedScale = 1f;
        public float MaximizedScale = 1.5f;

        void Start()
        {
            cameraMinimap = GetComponent<Camera>();
            //MinimapUI = GetComponentInParent<RectTransform>();

            if (rotateWithTarget)
            {
                cameraMinimap.transform.SetParent(targetFollow);
            }
        }

        void Update()
        {
            if (targetFollow != null && rotateWithTarget == false)
            {
                cameraMinimap.transform.position = new Vector3(targetFollow.position.x, ZoomOut, targetFollow.position.z);
                cameraMinimap.transform.eulerAngles = new Vector3(90, 0, 0);
            }

            CheckForInputs();
        }

        private void CheckForInputs()
        {
            if (Input.GetButton("General_MapZoomOut"))
            {
                ZoomOut += ZoomSpeed * Time.deltaTime;
                ZoomOut = Mathf.Clamp(ZoomOut, ZoomLowest, ZoomHighest);
            }
            else if (Input.GetButton("General_MapZoomIn"))
            {
                ZoomOut -= ZoomSpeed * Time.deltaTime;
                ZoomOut = Mathf.Clamp(ZoomOut, ZoomLowest, ZoomHighest);
            }

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
