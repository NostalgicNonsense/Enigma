using System.Collections;
using Assets.Enigma.Components.Base_Classes.Shells;
using UnityEngine;

public class CannonBase : MonoBehaviour
{
    public GameObject Shell;
    private bool _isReloading;

    private readonly ShellBase _shell;

    private float howMuchToRotateShellOnX = 90f;
	// Use this for initialization
	public void Start ()
	{
	    GetComponent<Collider>().enabled = false;
	}
	
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
        var shellInstance = Instantiate(Shell, transform);
        shellInstance.GetComponent<Rigidbody>().AddForce(transform.forward * 4200f);
        //_isReloading = true;
        WaitForReload();
    }

    private IEnumerator WaitForReload()
    {
        yield return new WaitForSeconds(5);
        _isReloading = false;
    }
}
