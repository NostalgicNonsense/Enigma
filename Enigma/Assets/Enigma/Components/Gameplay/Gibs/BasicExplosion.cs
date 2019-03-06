using UnityEngine;

namespace Enigma.Components.Gibs
{
    public class BasicExplosion : MonoBehaviour
    {
        public void Explode()
        {
            var explosion = GetComponent<ParticleSystem>();
            explosion.Play();
            Destroy(this);
        }
    }
}
