using System.Collections;
using Assets.Enigma.Components.Base_Classes.Shells;
using UnityEngine;

public class CannonBase : MonoBehaviour
{
    public GameObject Shell;
    private bool _isReloading;

    private readonly ShellBase _shell;
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
        var rotationToUse = new Quaternion(transform.rotation.x + 90f, transform.rotation.y, transform.rotation.z, transform.rotation.w);
        var shellInstance = Instantiate(Shell, transform.position, rotationToUse);
        shellInstance.GetComponent<Rigidbody>().velocity = new Vector3(4f, 4f, 0f);
        _isReloading = true;
        WaitForReload();
    }

    private IEnumerator WaitForReload()
    {
        yield return new WaitForSeconds(5);
        _isReloading = false;
    }
}
