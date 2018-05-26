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
        public Camera cameraCommander;
        private Boolean isRotating = false;

        void Start()
        {
        }

        public void SetSelectedHologram(BuildingHologram type)
        {
            BuildingStop();
            selectedHologram = Instantiate(type, type.transform.position, type.transform.rotation);
            selectedHologram.Enable();
        }

        /// <summary>
        /// Places building in world and drains cost.
        /// </summary>
        private void BuildingPlace()
        {
            if (selectedHologram != null && selectedHologram.IsAllowedPlacement)
            {
                var building = Instantiate(selectedHologram.BuildingCreate, selectedHologram.transform.position, GetRotationForPlacement(selectedHologram.transform.rotation));
                BuildingStop();
            }
            isRotating = false;
        }

        private Quaternion GetRotationForPlacement(Quaternion rotation)
        {
            var quaternion = rotation;
            var angle = quaternion.eulerAngles;
            quaternion.eulerAngles = new Vector3(0, angle.y, 0);
            return quaternion;
        }

        /// <summary>
        /// Stops rendering and returns cost.
        /// </summary>
        private void BuildingCancel()
        {
            //Todo: return cost
            BuildingStop();
        }

        private void BuildingStop()
        {
            if (selectedHologram != null)
            {
                selectedHologram.Disable();
                Destroy(selectedHologram);
            }
            isRotating = false;
        }

        private void BuildingRotate()
        {
            return; //Todo fix this!
            if (selectedHologram != null)
            {
                var direction = Vector3.RotateTowards(selectedHologram.transform.position, MousePosToWorld(), 11 * Time.deltaTime, 0.0f);
                selectedHologram.transform.rotation = GetRotationForPlacement(Quaternion.LookRotation(direction));
            }
        }


        void Update()
        {
            if (selectedHologram != null)
            {
                UpdateMovement();
                CheckMouseInput();

                if (isRotating)
                {
                    BuildingRotate();
                }
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
            //I'd actually ask me these questions instead of leaving breadcrumbs
            var pos = cameraCommander.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraCommander.nearClipPlane));
            var y = GetYPos(pos);
            return new Vector3(pos.x, y, pos.z);
        }

        private float GetYPos(Vector3 pos)
        {
            RaycastHit rayCast;
            Debug.DrawRay(pos, -transform.up, Color.cyan, 300f);
            Physics.Raycast(pos, -transform.up, out rayCast);
            Debug.Log("rayCast hit: " + rayCast.collider.name);

            //Todo: Add for loop here that checks for corners

            //Debug.Log("raycast Y: " + rayCast.point.y);
            return rayCast.point.y;

        }

    private void CheckMouseInput()
        {
            if (Input.GetButtonDown("Fire1")) //Place
            {
                isRotating = true;
            }
            else if (isRotating == true && Input.GetButtonUp("Fire1"))
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
