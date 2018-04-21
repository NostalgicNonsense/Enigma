using System;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts
{
    [System.Serializable]
    public class Turret : MonoBehaviour
    {
        public Collider turretBody;
        private const string MouseX = "Mouse X";
        private const string MouseY = "Mouse Y";

        public void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void FixedUpdate()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            Cursor.lockState = CursorLockMode.Locked;
            float speed = 20.0f;
            var horitzontalTransform = Input.GetAxis(MouseX) * speed * Time.deltaTime;
            var verticalTransform = Input.GetAxis(MouseY) * speed * Time.deltaTime * - 1;
            turretBody.transform.Rotate(new Vector3(verticalTransform, horitzontalTransform, 0));
        }

    }
}
