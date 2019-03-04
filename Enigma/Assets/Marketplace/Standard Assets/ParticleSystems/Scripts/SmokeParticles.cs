using UnityEngine;
using Random = UnityEngine.Random;

namespace Marketplace.Standard_Assets.ParticleSystems.Scripts
{
    public class SmokeParticles : MonoBehaviour
    {
        public AudioClip[] extinguishSounds;


        private void Start()
        {
            GetComponent<AudioSource>().clip = extinguishSounds[Random.Range(0, extinguishSounds.Length)];
            GetComponent<AudioSource>().Play();
        }
    }
}
