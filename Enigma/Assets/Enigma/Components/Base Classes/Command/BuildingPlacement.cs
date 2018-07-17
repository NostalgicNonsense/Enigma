using Assets.Enigma.Components.Base_Classes.Buildings;
using Assets.Enigma.Components.Base_Classes.TeamSettings.Resources;
using Assets.Enigma.Enums;
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

        public ResourceTeam ResourceManager;

        private RaycastHit rayY;

        void Start()
        {
        }

        public void SetSelectedHologram(BuildingHologram type)
        {
            BuildingStop();
            
            selectedHologram = Instantiate(type, type.transform.position, type.transform.rotation);
            selectedHologram.Enable();
            var stats = selectedHologram.BuildingCreate.BuildingStats;
            ResourceManager.Money.Reduce(stats.costMoney);
            ResourceManager.Oil.Reduce(stats.costOil);
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
            var stats = selectedHologram.BuildingCreate.BuildingStats;
            ResourceManager.Money.Add(stats.costMoney);
            ResourceManager.Oil.Add(stats.costOil);
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
            selectedHologram.transform.position = MousePosToWorld();
        }

        private Vector3 MousePosToWorld()
        {
            Plane plane = new Plane(Vector3.up, 0);

            float dist;
            Ray ray = cameraCommander.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out dist))
            {
                Vector3 point = ray.GetPoint(dist);
                return new Vector3(point.x, GetYPos(point), point.z);
            }
            return Vector3.zero;
        }

        private float GetYPos(Vector3 pos)
        {
            RaycastHit rayCast;
            Debug.DrawRay(pos, -transform.up, Color.cyan, 300f);
            Physics.Raycast(pos, -transform.up, out rayCast);

            rayY = rayCast;
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
