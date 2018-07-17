using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Gibs
{
    public class GibFadeout : MonoBehaviour
    {
        [SerializeField]
        private float AlphaPerFrame = 0.001f;
        private bool destroy = false;
        private float alpha = 1f;

        private Rigidbody[] rigidbodies;
        private MeshRenderer[] meshRenderers;
        
        void Start()
        {
            rigidbodies = GetComponentsInChildren<Rigidbody>();
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
        }

        void Update()
        {
            if (!destroy)
            {
                alpha -= AlphaPerFrame;
                if (alpha <= 0)
                {
                    destroy = true;
                    Destroy(gameObject);
                }
                else
                {
                    foreach (var rig in meshRenderers)
                    {
                        rig.material.color = new Color(rig.material.color.r, rig.material.color.g, rig.material.color.b, alpha);
                    }
                }
            }
        }
    }
}
