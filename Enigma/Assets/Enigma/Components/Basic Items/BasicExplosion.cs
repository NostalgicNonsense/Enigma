using UnityEngine;

namespace Assets.Enigma.Components.Basic_Items
{
    public class BasicExplosion : MonoBehaviour
    {
        public void Explode()
        {
            var explosion = GetComponent<ParticleSystem>();
            explosion.Play();
            Destroy(this, explosion.main.duration);
        }
    }
}
