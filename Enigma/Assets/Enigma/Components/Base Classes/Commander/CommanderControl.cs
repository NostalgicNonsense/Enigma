using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Commander
{
    public class CommanderControl : MonoBehaviour
    {

        public float cameraSpeedHorizontal;
        public float cameraSpeedVertical;
        public float cameraSpeedZoom;
        public float cameraSpeedRotation;

        private float x;
        private float y;
        private float z;

        private float rotation;

        private Camera cameraCommander;
       
        // Use this for initialization
        void Start()
        {
            cameraCommander = GetComponent<Camera>();
        }

        // Update is called once per frame
        void Update()
        {
            CheckInputs();
        }

        private void CheckInputs()
        {
            x = Input.GetAxis("Horizontal") * Time.deltaTime * cameraSpeedHorizontal;
            z = Input.GetAxis("Vertical") * Time.deltaTime * cameraSpeedVertical;
            y = Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * cameraSpeedZoom;
            rotation = Input.GetAxis("Rotate") * Time.deltaTime * cameraSpeedRotation;

            transform.Translate(transform.right * x, Space.World);
            transform.Translate(transform.forward * z, Space.World);
            transform.Translate(transform.up * -y, Space.World); //Todo add min y

            transform.Rotate(new Vector3(0, transform.position.y, 0), rotation);
        }
    }
}
