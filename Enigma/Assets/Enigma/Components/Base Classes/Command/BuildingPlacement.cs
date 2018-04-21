using Assets.Enigma.Components.Base_Classes.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Commander
{
    public class BuildingPlacement : MonoBehaviour
    {
        private BuildingHologram selectedHologram;
        private Camera cameraCommander;

        void Start()
        {
            cameraCommander = GetComponentInChildren<Camera>();
        }

        public void SetSelectedHologram(BuildingHologram type)
        {
            BuildingStop();
            selectedHologram = type;
            selectedHologram.Enable();
        }

        /// <summary>
        /// Places building in world and drains cost.
        /// </summary>
        private void BuildingPlace()
        {
            if (selectedHologram != null && selectedHologram.IsAllowedPlacement)
            {
                var building = Instantiate(selectedHologram.BuildingCreate, selectedHologram.transform.position, selectedHologram.transform.rotation);
                BuildingStop();
            }
        }

        /// <summary>
        /// Stops rendering and returns cost.
        /// </summary>
        private void BuildingCancel()
        {
            BuildingStop();
        }

        private void BuildingStop()
        {
            if (selectedHologram != null)
            {
                selectedHologram.Disable();
                selectedHologram = null;
            }
        }

        private void BuildingRotate()
        {
            if (selectedHologram != null)
            {
                var direction = Vector3.RotateTowards(selectedHologram.transform.position, MousePosToWorld(), 11 * Time.deltaTime, 0.0f);
                selectedHologram.transform.rotation = Quaternion.LookRotation(direction);
            }
        }


        void Update()
        {
            if (selectedHologram != null)
            {
                UpdateMovement();
                CheckMouseInput();
            }
        }

        private void UpdateMovement()
        {
            var mousePos = MousePosToWorld();
            selectedHologram.transform.position = mousePos;
            //Debug.Log("hologram pos: " + selectedHologram.transform.position);
        }

        private Vector3 MousePosToWorld()
        {
            // var pos = cameraCommander.ScreenToWorldPoint(Input.mousePosition);
            //Debug.Log(" Input.mousePosition: " + Input.mousePosition);
            //Debug.Log(" pos: " + pos);
            //why doesn't this work Jake!? :o
            var pos = cameraCommander.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraCommander.nearClipPlane));
            var y = 0;
            return new Vector3(pos.x, y, pos.z);
        }

        private void CheckMouseInput()
        {
            if (Input.GetButton("Fire1")) //Place
            {
                BuildingRotate();
            }
            else if (Input.GetButtonUp("Fire1"))
            {
                BuildingPlace();
            }

            if (Input.GetButton("Fire2")) //Cancel
            {
                BuildingCancel();
            }
        }
    }
}
