using Assets.Enigma.Enums;
using System;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings
{
    public class BuildingHologram : MonoBehaviour
    {
        public Building BuildingCreate;
        public Boolean IsAllowedPlacement { get; private set; }
        public MeshRenderer MeshRenderer { get; private set; }
        private int collisions = 0;

        void Start()
        {
            MeshRenderer = GetComponent<MeshRenderer>();
            IsAllowedPlacement = true;
        }

        public void Enable()
        {
            IsAllowedPlacement = true;
            MeshRenderer = GetComponent<MeshRenderer>();
            MeshRenderer.enabled = true;
        }

        public void Disable()
        {
            MeshRenderer.enabled = false;
        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log("other tag: " + other.tag);
            if (!other.isTrigger && other.tag != EnigmaTags.Water.ToString() && other.tag != EnigmaTags.Debris.ToString())
            {
                IsAllowedPlacement = false;
                collisions++;
                SetCollisionColor();
            }
        }

        void OnTriggerExit()
        {
            collisions--;
            if (collisions <= 0)
            {
                IsAllowedPlacement = true;
                SetCollisionColor();
            }
        }

        private void SetCollisionColor()
        {
            if (IsAllowedPlacement == false)
            {
                MeshRenderer.material.color = Color.red;
            }
            else
            {
                MeshRenderer.material.color = Color.green;
            }
        }
    }
}
