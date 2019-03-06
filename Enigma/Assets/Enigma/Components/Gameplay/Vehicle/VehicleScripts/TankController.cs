using System;
using Enigma.Components.Gameplay.Player;
using Enigma.Components.Gameplay.Vehicle.ComponentScripts;
using Enigma.Components.Gameplay.Vehicle.VehicleWeapons;
using Enigma.Enums;
using UnityEngine;

namespace Enigma.Components.Gameplay.Vehicle.VehicleScripts
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
                if (steering > 0)
                {
                    LeftTrack.ApplyTorque(motor);
                }
                else
                {
                    RightTrack.ApplyTorque(motor);
                }
                // To turn left/right without going forward
            }

            else if (motor != 0)
            {
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
    }

    [Serializable]
    public class Track
    {
        public Collider TrackBody;

        public void ApplyTorque(float torqueAmount)
        {
            TrackBody.attachedRigidbody.AddForce(new Vector3(0, 0, -1) * torqueAmount, ForceMode.Acceleration); // Because tracks are facing up
        }
        //TODO: Add the ability to destroy these with shells!
    }
}
