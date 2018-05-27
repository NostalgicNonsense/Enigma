using System;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts
{
    [System.Serializable]
    public class Turret : MonoBehaviour
    {
        public GameObject TurretBody;
        public GameObject TurretBarrel;
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
            const float speed = 20.0f;
            var horitzontalTransform = Input.GetAxis(MouseX) * speed * Time.deltaTime;
            var verticalTransform = Input.GetAxis(MouseY) * speed * Time.deltaTime * - 1;
            //TurretBody.transform.Rotate(new Vector3(TurretBody.transform.position.z, horitzontalTransform, 0));
            TurretBody.transform.Rotate(new Vector3(transform.rotation.x, transform.rotation.y, horitzontalTransform));
            TurretBarrel.transform.Rotate(verticalTransform, transform.rotation.y, transform.rotation.z);
        }

    }
}
