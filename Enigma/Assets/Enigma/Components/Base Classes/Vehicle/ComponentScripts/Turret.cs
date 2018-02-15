using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts
{
    [System.Serializable]
    public class Turret : MonoBehaviour
    {
        public Rigidbody turretBody;

        public void Update()
        {
            float speed = 1.0f;
            turretBody.transform.Rotate(new Vector3(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0) * Time.deltaTime * speed);
        }

    }
}
