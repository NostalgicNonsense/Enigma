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
        public Building Barrack;
        private Camera cameraCommander;
        MeshRenderer meshRenderer;
        MeshCollider meshCollider;
        Boolean isAllowedPlacement;

        void Start()
        {
            cameraCommander = GetComponentInChildren<Camera>();
        }

        public void SetSelectedHologram(BuildingHologram type)
        {
            selectedHologram = type;
            meshRenderer = selectedHologram.GetComponent<MeshRenderer>();
            meshCollider = selectedHologram.GetComponent<MeshCollider>();

            meshRenderer.enabled = true;
        }

        /// <summary>
        /// Places building in world and drains cost.
        /// </summary>
        private void BuildingPlace()
        {
            if (isAllowedPlacement)
            {
                var building = Instantiate(Barrack, selectedHologram.transform.position, selectedHologram.transform.rotation);
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
            meshRenderer.enabled = false;
            selectedHologram = null;
        }
   

        void Update()
        {
            if (selectedHologram != null)
            {
                UpdateMovement();
                CheckCollision();
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

        private void CheckCollision()
        {
            if (isAllowedPlacement == false)
            {
                meshRenderer.material.color = Color.red;
            }
            else
            {
                meshRenderer.material.color = Color.green;
            }
        }

        private void CheckMouseInput()
        {
            if (Input.GetButton("Fire1")) //Place
            {
                BuildingPlace();
            }
            else if (Input.GetButton("Fire2")) //Cancel
            {
                BuildingCancel();
            }
        }
    }
}
