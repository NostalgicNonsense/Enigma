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
        public float RotationSpeed = 20.0f;

        public float BarrelXMin = -10;
        public float BarrelXMax = 12;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        void FixedUpdate()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            Cursor.lockState = CursorLockMode.Locked;

            UpdateRotation();
        }

        private void UpdateRotation()
        {
            var horizontal = Input.GetAxis(MouseX) * RotationSpeed * Time.deltaTime;
            var vertical = Input.GetAxis(MouseY) * RotationSpeed * Time.deltaTime * -1;

            TurretBody.transform.Rotate(new Vector3(0, 0, horizontal));

            vertical = GetMaxVertical(vertical);
            TurretBarrel.transform.Rotate(vertical, 0, 0);
        }

        private float GetMaxVertical(float vertical)
        {
            //Todo Mathf.Clamp rotation somehow
            //Todo: This can be done much better
            //if (vertical < 0)
            //{
            //    var rot = TurretBarrel.transform.rotation.eulerAngles.x;
            //    if (rot + vertical < BarrelXMin)
            //    {
            //        return 0;
            //    }
            //}
            //else if (vertical > 0)
            //{
            //    var rot = TurretBarrel.transform.rotation.eulerAngles.x;
            //    if (rot - vertical > BarrelXMax)
            //    {
            //        return 0;
            //    }
            //}
            //Dz tired now
            return vertical;
        }
    }
}
