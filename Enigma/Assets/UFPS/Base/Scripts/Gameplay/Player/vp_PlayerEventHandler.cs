/////////////////////////////////////////////////////////////////////////////////
//
//	vp_PlayerEventHandler.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this class declares events for communication between behaviours
//					that make up a basic player object. it also binds several object
//					component states to player activity events
//
///////////////////////////////////////////////////////////////////////////////// 

using System;
using UnityEngine;

public class vp_PlayerEventHandler : vp_StateEventHandler
{

	// these declarations determine which events are supported by the
	// player event handler. it is then up to external classes to fill
	// them up with delegates for communication.

	// TIPS:
	//  1) mouse-over on the event types (e.g. vp_Message) for usage info.
	//  2) to find the places where an event is SENT, you can do 'Find All
	// References' on the event in your IDE. if this is not available, you
	// can search the project for the event name preceded by '.' (.Reload)
	//  3) to find the methods that LISTEN to an event, search the project
	// for its name preceded by '_' (_Reload)

	// player type
	public vp_Value<bool> IsFirstPerson;	// always returns true if this a local player in 1st person mode, false if 3rd person mode or multiplayer remote player or AI
	public vp_Value<bool> IsLocal;			// returns true if a vp_FPCamera is present on this player
	public vp_Value<bool> IsAI;				// should return true if this player is controlled by an AI script

	// health
	public vp_Value<float> Health;
	public vp_Value<float> MaxHealth;

	// position and rotation
	public vp_Value<Vector3> Position;
	public vp_Value<Vector2> Rotation;		// world XY (pitch, yaw) rotation of head
	public vp_Value<float> BodyYaw;			// world Y (yaw) rotation of lower body

	// headlook
	public vp_Value<Vector3> LookPoint;
	public vp_Value<Vector3> HeadLookDirection;		// head forward vector. NOTE: this will be different from 'CameraLookDirection' in 3rd person
	public vp_Value<Vector3> AimDirection;			// direction between weapon and lookpoint. NOTE: this is different from direction between head and lookpoint

	// motor
	public vp_Value<Vector3> MotorThrottle;
	public vp_Value<bool> MotorJumpDone;

	// input
	public vp_Value<Vector2> InputMoveVector;
	public vp_Value<float> InputClimbVector;

	// activities
	public vp_Activity Dead;
	public vp_Activity Run;
	public vp_Activity Jump;
	public vp_Activity Crouch;
	public vp_Activity Zoom;
	public vp_Activity Attack;
	public vp_Activity Reload;
	public vp_Activity Climb;
	public vp_Activity Interact;
	public vp_Activity<int> SetWeapon;
	public vp_Activity OutOfControl;

	// weapon object events
	public vp_Message<int> Wield;
	public vp_Message Unwield;
	public vp_Attempt Fire;
	public vp_Message DryFire;

	// weapon handler events
	public vp_Attempt SetPrevWeapon;
	public vp_Attempt SetNextWeapon;
	public vp_Attempt<string> SetWeaponByName;
	[Obsolete("Please use the 'CurrentWeaponIndex' vp_Value instead.")]
	public vp_Value<int> CurrentWeaponID;	// renamed to avoid confusion with vp_ItemType.ID
	public vp_Value<int> CurrentWeaponIndex;
	public vp_Value<string> CurrentWeaponName;
	public vp_Value<bool> CurrentWeaponWielded;
	public vp_Attempt AutoReload;
	public vp_Value<float> CurrentWeaponReloadDuration;

	// inventory
	public vp_Message<string, int> GetItemCount;
	public vp_Attempt RefillCurrentWeapon;
	public vp_Value<int> CurrentWeaponAmmoCount;
	public vp_Value<int> CurrentWeaponMaxAmmoCount;
	public vp_Value<int> CurrentWeaponClipCount;
	public vp_Value<int> CurrentWeaponType;
	public vp_Value<int> CurrentWeaponGrip;
	public vp_Attempt<object> AddItem;
	public vp_Attempt<object> RemoveItem;
	public vp_Attempt DepleteAmmo;

	// physics
	public vp_Message<Vector3> Move;
	public vp_Value<Vector3> Velocity;
	public vp_Value<float> SlopeLimit;
	public vp_Value<float> StepOffset;
	public vp_Value<float> Radius;
	public vp_Value<float> Height;
	public vp_Value<float> FallSpeed;
	public vp_Message<float> FallImpact;
	public vp_Message<float> HeadImpact;
	public vp_Message<Vector3> ForceImpact;
	public vp_Message Stop;
	public vp_Value<Transform> Platform;
	public vp_Value<Vector3> PositionOnPlatform;
	public vp_Value<bool> Grounded;

	// interaction
	public vp_Value<vp_Interactable> Interactable;
	public vp_Value<bool> CanInteract;


	/// <summary>
	/// 
	/// </summary>
	protected override void Awake()
	{

		base.Awake();

		// TIP: please see the manual for the difference
		// between (player) activities and (component) states

		// --- activity state bindings ---
		// whenever these activities are toggled they will enable and
		// disable any component states with the same names. disable
		// these lines to make states independent of activities
		BindStateToActivity(Run);
		BindStateToActivity(Jump);
		BindStateToActivity(Crouch);
		BindStateToActivity(Zoom);
		BindStateToActivity(Reload);
		BindStateToActivity(Dead);
		BindStateToActivity(Climb);
		BindStateToActivity(OutOfControl);
		BindStateToActivityOnStart(Attack);	// <--
		// in this default setup the 'Attack' activity will enable
		// - but not disable - the component attack states when toggled.

		// --- activity AutoDurations ---
		// automatically stops an activity after a set timespan
		SetWeapon.AutoDuration = 1.0f;		// NOTE: altered at runtime by each weapon
		Reload.AutoDuration = 1.0f;			// NOTE: altered at runtime by each weapon

		// --- activity MinDurations ---
		// prevents player from aborting an activity too soon after starting
		Zoom.MinDuration = 0.2f;
		Crouch.MinDuration = 0.5f;

		// --- activity MinPauses ---
		// prevents player from restarting an activity too soon after stopping
		Jump.MinPause = 0.0f;			// increase this to enforce a certain delay between jumps
		SetWeapon.MinPause = 0.2f;

	}


}

