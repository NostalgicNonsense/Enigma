using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Enigma.Components.Base_Classes.Player;
using Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts;
using Assets.Enigma.Enums;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.VehicleScripts
{
    public class TankController : MonoBehaviour, IVehicle
    {
        public Track LeftTrack;
        public Track RightTrack;
        public float MaxMotorTorque;
        private Turret _turret;
        private IPlayer _player;
        private CannonBase _cannonBase;

        public void Start()
        {
            gameObject.tag = GameEntityType.Vehicle.ToString();
            _turret = GetComponentInChildren<Turret>();
            _turret.enabled = false;

            _cannonBase = GetComponentInChildren<CannonBase>();
            _cannonBase.enabled = false;
        }

        public void SetPlayerOccupant(IPlayer player)
        {
            Debug.Log("TankController, SettingPlayer");
            _player = player;

            _turret.enabled = true;
            GetComponentInChildren<CannonBase>().enabled = true;
            GetComponentInChildren<Turret>().enabled = true;
        }

        public void FixedUpdate()
        {
            if (_player == null)
            {
                return;
            }

            var motor = MaxMotorTorque * Input.GetAxis("Vertical");
            var steering = Input.GetAxis("Horizontal");
            if (motor == 0 && steering != 0)
            {
                motor = MaxMotorTorque; // To turn left/right without going forward
            }
            var leftSteer = motor;
            var rightSteer = motor;
            if (steering > 0)
            {
                rightSteer = motor * -1;
            }
            else if (steering < 0)
            {
                leftSteer = motor * -1;
            }
            
            LeftTrack.ApplyTorque(leftSteer);
            RightTrack.ApplyTorque(rightSteer);
        }
    }

    [Serializable]
    public class Track
    {
        public List<WheelCollider> WheelColliders;

        public void ApplyTorque(float torque)
        {
            foreach (var wheelCollider in WheelColliders)
            {
                wheelCollider.motorTorque = torque;
            }
        }
    }
}
