using System.Collections;
using Enigma.Components.Base_Classes.Launchables;
using UnityEngine;

namespace Enigma.Components.Base_Classes.Vehicle.VehicleWeapons
{
    public class CannonBase : MonoBehaviour
    {
        public GameObject Shell;
        private bool _isReloading;

        public AudioSource SoundReload;

        private readonly ShellBase _shell;
        private float howMuchToRotateShellOnX = 90f;
	
        // Update is called once per frame
        public void FixedUpdate ()
        {
            if(!_isReloading && Input.GetMouseButtonDown(0))
            {
                Fire();
            }
        }

        private void Fire()
        {
            var shellInstance = Instantiate(Shell, transform.position, transform.rotation);
            shellInstance.GetComponent<Rigidbody>().AddForce(transform.forward * 12000f);
            //_isReloading = true;
            StartCoroutine(FireDelay());
        }

        private IEnumerator FireDelay()
        {
            yield return new WaitForSeconds(1);
            _isReloading = false;
        }
    }
}
