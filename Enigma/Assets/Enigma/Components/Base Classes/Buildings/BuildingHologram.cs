using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings
{
    public class BuildingHologram : MonoBehaviour
    {
        public Building BuildingCreate;
        public Boolean IsAllowedPlacement { get; private set; }
        public MeshRenderer meshRenderer { get; private set; }
        private int collisions = 0;

        void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();

        }

        public void Enable()
        {
            meshRenderer.enabled = true;
        }

        public void Disable()
        {
            meshRenderer.enabled = false;
        }

        void OnTriggerEnter(Collider other)
        {
            IsAllowedPlacement = false;
            collisions++;
            CheckCollision();
        }

        void OnTriggerExit()
        {
            collisions--;
            if (collisions <= 0)
            {
                IsAllowedPlacement = true;
                CheckCollision();
            }
        }

        private void CheckCollision()
        {
            if (IsAllowedPlacement == false)
            {
                meshRenderer.material.color = Color.red;
            }
            else
            {
                meshRenderer.material.color = Color.green;
            }
        }
    }
}
