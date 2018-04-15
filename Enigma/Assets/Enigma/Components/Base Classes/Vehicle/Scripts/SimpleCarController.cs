using UnityEngine;
using System.Collections.Generic;
using Assets.Enigma.Components.Base_Classes.Player;
using Assets.Enigma.Components.Base_Classes.Vehicle.ComponentScripts;
using Assets.Enigma.Enums;

public class SimpleCarController : MonoBehaviour
{
    // Currently this is specifically geared towards Tanks. in v2 I'll fix
    public List<AxleInfo> axleInfos;
    public float maxMotorTorque;
    public float maxSteeringAngle;
    public Turret turret;
    private IPlayer Player;

    public void Start()
    {
        gameObject.tag = GameEntityType.Vehicle.ToString();
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
    }

    public void FixedUpdate()
    {
        if (Player == null)
        {
            return;
        }
        var motor = maxMotorTorque * Input.GetAxis("Vertical");
        var steering = maxSteeringAngle * Input.GetAxis("Horizontal");

        turret.FixedUpdate();

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

