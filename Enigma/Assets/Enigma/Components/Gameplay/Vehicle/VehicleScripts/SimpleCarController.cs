using System.Collections.Generic;
using Enigma.Components.Gameplay.Player;
using Enigma.Components.Gameplay.Vehicle.ComponentScripts;
using Enigma.Components.Gameplay.Vehicle.VehicleWeapons;
using Enigma.Enums;
using UnityEngine;

namespace Enigma.Components.Gameplay.Vehicle.VehicleScripts
{
    public class SimpleCarController : MonoBehaviour , IVehicle
    {
        // Currently this is specifically geared towards Tanks. in v2 I'll fix
        public List<AxleInfo> axleInfos;
        public float maxMotorTorque;
        public float maxSteeringAngle;
        private Turret turret;
        private IPlayer Player;
        private CannonBase cannonBase;

        public void Start()
        {
            gameObject.tag = GameEntityType.Vehicle.ToString();
            turret = GetComponentInChildren<Turret>();
            turret.enabled = false;

            cannonBase = GetComponentInChildren<CannonBase>();
            cannonBase.enabled = false;
        }
        // finds the corresponding visual wheel
        // correctly applies the transform
        public void ApplyLocalPositionToVisuals(WheelCollider collider)
        {
            if (collider.transform.childCount == 0)
            {
                return;
            }

            var visualWheel = collider.transform.GetChild(0);

            Vector3 position;
            Quaternion rotation;
            collider.GetWorldPose(out position, out rotation);

            visualWheel.transform.position = position;
            visualWheel.transform.rotation = rotation;
        }

        public void SetPlayerOccupant(IPlayer player)
        {
            Debug.Log("Simplecarcontroller, SettingPlayer");
            Player = player;
        
            turret.enabled = true;
            GetComponentInChildren<CannonBase>().enabled = true;
            GetComponentInChildren<Turret>().enabled = true;
        }

        /// <summary>
        /// Todo: Make person eject
        /// </summary>
        public void EjectPassangers()
        {
        
        }

        public void FixedUpdate()
        {
            if (Player == null)
            {
                return;
            }
            var motor = maxMotorTorque * Input.GetAxis("Vertical");
            var steering = maxSteeringAngle * Input.GetAxis("Horizontal");

            //turret.FixedUpdate(); //FixedUpdate is a Unity built in function, it calls itself from the monoBehaviour

            foreach (var axleInfo in axleInfos)
            {
                if (axleInfo.steering)
                {
                    axleInfo.leftWheel.steerAngle = steering;
                    axleInfo.rightWheel.steerAngle = steering;
                }
                if (axleInfo.motor)
                {
                    axleInfo.leftWheel.motorTorque = motor;
                    axleInfo.rightWheel.motorTorque = motor;
                }
                ApplyLocalPositionToVisuals(axleInfo.leftWheel);
                ApplyLocalPositionToVisuals(axleInfo.rightWheel);
            }
        }
    }

    [System.Serializable]
    public class AxleInfo
    {
        public WheelCollider leftWheel;
        public WheelCollider rightWheel;
        public bool motor; // is this wheel attached to motor?
        public bool steering; // does this wheel apply steer angle?
    }
}