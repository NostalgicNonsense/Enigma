using Assets.Enigma.Components.Base_Classes.TeamSettings;
using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
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
            cameraCommander = GetComponentInChildren<Camera>();
        }

        // Update is called once per frame
        void Update()
        {
            CheckInputs();
        }

        private void CheckInputs()
        {

            x = Input.GetAxis("Horizontal") * cameraSpeedHorizontal * Time.deltaTime;
            z = Input.GetAxis("Vertical") * cameraSpeedVertical * Time.deltaTime;
            y = Input.GetAxis("Mouse ScrollWheel") * cameraSpeedZoom * Time.deltaTime;

            transform.Translate(transform.right * x, Space.World);
            transform.Translate(transform.forward * z, Space.World);
            transform.Translate(transform.up * -y, Space.World); //Todo add min y
            //cameraCommander.orthographicSize = cameraCommander.orthographicSize - y;

            transform.Rotate(new Vector3(0, transform.position.y, 0), rotation);
        }
    }
}
