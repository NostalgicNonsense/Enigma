/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPRemotePlayer.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this class represents a remote player in multiplayer. it extends
//					vp_MPNetworkPlayer with all the functionality specific to remote
//					controlled player gameobjects in the scene. in essence, it listens
//					to the activity of a certain player in the photon cloud and uses
//					it to move, rotate and animate our own scene 'puppet player'.
//					this includes interpreting fire events in a way that is hard to
//					exploit for cheating purposes, and converting incoming RPCs	to
//					events in the vp_PlayerEventHandler (such as crouching, aiming,
//					running and reloading). there can be an arbitrary number of remote
//					player objects in a scene.
//
//					NOTE: this class is always under construction and has experimental
//					functionality to predict player position and simulate lag
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

public class vp_MPRemotePlayer : vp_MPNetworkPlayer
{

	// latest 'real' position, rotation and velocity received over network
	protected Vector3 m_LastPosition = Vector3.zero;
	protected Vector2 m_LastRotation = Vector2.zero;
	protected Vector3 m_LastVelocity = Vector3.zero;

	// variables for interpolated position, rotation and velocity
	protected Vector2 m_SmoothRotation = Vector2.zero;
	protected Vector3 m_CurrentPosition = Vector3.zero;
	protected Vector2 m_CurrentRotation = Vector2.zero;
	protected Vector3 m_CurrentVelocity = Vector3.zero;
	protected Vector3 m_TempPosition = (Vector3.up * 1000);		// for instantiating new player objects out-of-the-way
	protected Vector3 m_LastPlatformPos = Vector3.zero;
	public Vector2 MaxDeviation = new Vector2(5.0f, 1.0f);		// prevents the remote player from deviating more than X meters from the latest incoming
																// horizontal network position, and caps vertical transform position to between ground
																// and Y meters above that position

	// ground snap
	public float GroundSnapRange = 0.5f;						// if within ground range and falling, transform will be smooth-snapped to ground
	public const int GroundSnapMask = ~((1 << vp_Layer.LocalPlayer) | (1 << vp_Layer.Debris) |
								(1 << vp_Layer.IgnoreRaycast) | (1 << vp_Layer.Trigger) |
								(1 << vp_Layer.RemotePlayer) | (1 << vp_Layer.Ragdoll) |
								(1 << vp_Layer.Water) | (1 << vp_Layer.MovableObject) | (1 << vp_Layer.Pickup));		// excludes movableobjects for smoother physics around rigidbodies
	protected RaycastHit m_AltitudeHit;							// used to detect the current ground altitude
	protected float m_GroundAltitude = 0.0f;

	// firing
	protected List<FireEvent> FireEvents = new List<FireEvent>();
	protected GameObject m_BulletAdjuster;				// used for avoiding shooting ourselves under lagged conditions
	protected Transform m_BulletAdjusterTransform;		// TODO: need to cache both gameobject & transform?
	protected RaycastHit m_BulletAdjusterHit;
	protected int m_RemoteWeaponIndex = 0;

	// animation
	protected bool m_IsAnimated = true;

	// platforms
	private int m_PlatformIDLastFrame = 0;
	private int m_PlatformID = 0;

	// prediction & lag simulation
	public bool PredictPosition = true;			// if enabled, velocity will be used to mask lag by predicting next position every frame
	public int SimulateLostFrames = 0;			// can be used to test prediction algorithms by ignoring a certain amount of incoming position updates
	public bool ShowNetworkPosition = false;	// if enabled, shows a transparent capsule collider representing the position updates we are actually receiving from photon
	
	protected int m_FramesToDrop = 0;
	protected GameObject m_NetworkPositionMarker = null;
	protected GameObject NetworkPositionMarker
	{
		get
		{
			if (m_NetworkPositionMarker == null)
				m_NetworkPositionMarker = vp_3DUtility.DebugPrimitive(PrimitiveType.Capsule, Vector3.one, new Color(1, 1, 1, 0.2f), Vector3.up, Transform);
			return m_NetworkPositionMarker;
		}
	}

	// nametag
	vp_NameTag m_NameTag = null;
	public vp_NameTag NameTag
	{
		get
		{
			if (m_NameTag == null)
				m_NameTag = transform.root.GetComponentInChildren<vp_NameTag>();
			return m_NameTag;
		}
	}

	// initialization
	protected bool m_StartedLerping
	{
		get
		{
			return (Time.time > m_TimeOfBirth + 0.5f);
		}
	}
	protected float m_TimeOfBirth = 0.0f;

	// constants
	private const float CLIMB_INTERPOLATION_SPEED = 10.0f;
	private const float PLATFORM_INTERPOLATION_SPEED = 10.0f;


	/// <summary>
	/// this class represents a logged weapon discharge, and is used to
	/// prevent firing rate hacks. see the 'UpdateFiring' method for
	/// more info
	/// </summary>
	public class FireEvent
	{

		public int WeaponIndex = 0;
		public Vector3 Position = Vector3.zero;
		public Quaternion Rotation = Quaternion.identity;
		public int Seed = 0;

		public FireEvent(int weaponIndex, Vector3 position, Quaternion rotation, int fireSeed)
		{
			WeaponIndex = weaponIndex;
			Position = position;
			Rotation = rotation;
			Seed = fireSeed;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public override void Awake()
	{

		base.Awake();

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		if (Player != null)
			Player.Register(this);

		m_TimeOfBirth = Time.time;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		if (Player != null)
			Player.Unregister(this);

	}


	/// <summary>
	/// 
	/// </summary>
	public override void Start()
	{

		base.Start();

		// bullet adjuster is used to prevent shooting ourselves under
		// lagged conditions
		m_BulletAdjuster = new GameObject("FireTransform");
		m_BulletAdjuster.transform.parent = transform;
		m_BulletAdjusterTransform = m_BulletAdjuster.transform;
		vp_Utility.Activate(m_BulletAdjuster, false);

		// this makes pre-existing remote players snap to their correct
		// position when we join a game
		SetPosition(m_TempPosition, Quaternion.identity);	// move out of world initially ...

	}


	/// <summary>
	/// scans this player hierarchy for shooter components, hooks up their
	/// fire seed, position and rotation with data from the 'FireEvents'
	/// array and puts them in a dictionary for validation later
	/// </summary>
	public override void InitShooters()
	{
		
		vp_WeaponShooter[] shooters = gameObject.GetComponentsInChildren<vp_WeaponShooter>(true) as vp_WeaponShooter[];
		m_Shooters.Clear();
		foreach (vp_WeaponShooter f in shooters)
		{
			vp_WeaponShooter shooter = f;	// need to cache shooter or delegate will only be set with values of last weapon

			if (m_Shooters.ContainsKey(WeaponHandler.GetWeaponIndex(shooter.Weapon)))
				continue;
			//Debug.Log("setting 'GetFireRotation' for: " + f);

			shooter.GetFireSeed = delegate
			{
				return FireEvents[0].Seed;
			};
			shooter.GetFirePosition = delegate
			{
				return FireEvents[0].Position;
			};
			shooter.GetFireRotation = delegate
			{
				return FireEvents[0].Rotation;
			};

			m_Shooters.Add(WeaponHandler.GetWeaponIndex(shooter.Weapon), shooter);

		}
	
	}


	/// <summary>
	/// gets the height of the ground immediately below this player
	/// </summary>
	protected virtual void StoreGroundAltitude()
	{

		// spherecast from waist and ten meters down to store the current altitude
		if(Physics.SphereCast(new Ray(Transform.position + Vector3.up, Vector3.down),
					0.4f, out m_AltitudeHit, 10.0f,
					GroundSnapMask))
			m_GroundAltitude = m_AltitudeHit.point.y;
		else
			m_GroundAltitude = -100000.0f;

	}


	/// <summary>
	/// 
	/// </summary>
	protected int PlatformID
	{

		set
		{

			if (value == m_PlatformIDLastFrame)
				return;

			Transform platform = vp_MPMaster.GetTransformOfViewID(value);
			if ((platform != null)
				&& ((platform.GetComponent<Collider>() != null)
				//&& (this.IsCloseTo(platform.collider))
				))
			{
				Player.Platform.Set(platform);
				m_PlatformID = value;
			}
			else
			{
				Player.Platform.Set(null);
				m_PlatformID = 0;
			}

		}

		get
		{
			return m_PlatformID;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public override void Update()
    {

		if (photonView.isMine)
			return;
		
		m_LastPlatformPos = m_CurrentPosition;

		UpdateNetworkValues();

		UpdatePosition();

		UpdateVelocity();

		UpdateRotation();

		UpdateDebugPrimitive();

		UpdateFiring();

	}


	/// <summary>
	/// for testing prediction algorithms
	/// </summary>
	protected virtual void UpdateNetworkValues()
	{

		// if we are not simulating lag, always use the most up-to-date
		// position and velocity values
		if (SimulateLostFrames < 1)
		{
			m_CurrentPosition = m_LastPosition;
			m_CurrentVelocity = m_LastVelocity;
			m_CurrentRotation = m_LastRotation;
		}
		else
		{
			// we are simulating lag, so only periodically update position and velocity
			if (m_FramesToDrop <= 0)
			{
				m_CurrentPosition = m_LastPosition;
				m_CurrentVelocity = m_LastVelocity;
				m_CurrentRotation = m_LastRotation;
				m_FramesToDrop = SimulateLostFrames;
			}
			m_FramesToDrop--;
		}

	}


	/// <summary>
	/// for testing prediction algorithms
	/// </summary>
	protected virtual void UpdateDebugPrimitive()
	{

		// if applicable, draw a debug capsule showing the last known network position
		if (ShowNetworkPosition)
		{
			NetworkPositionMarker.transform.position = m_CurrentPosition;
			if (!NetworkPositionMarker.activeSelf)
				vp_Utility.Activate(NetworkPositionMarker);
		}
		else if (m_NetworkPositionMarker != null)	// NOTE: _not_ polling the _property_ is intended here - we don't want it to initialize
			vp_Utility.Activate(NetworkPositionMarker, false);

	}


	/// <summary>
	/// this is used to prevent remote players from spawning with a
	/// falling animation. when they spawn at their temp (free fall)
	/// position, animators will get immediately paused and forcibly
	/// 'grounded' and unpaused as soon as they spawn properly
	/// </summary>
	public virtual void SetAnimated(bool value)
	{

		m_IsAnimated = value;

		vp_RagdollHandler v = transform.root.GetComponentInChildren<vp_RagdollHandler>();
		if (v != null)
			v.enabled = value;

		vp_BodyAnimator b = transform.root.GetComponentInChildren<vp_BodyAnimator>();
		if (b != null)
			b.enabled = value;

		Animator a = transform.root.GetComponentInChildren<Animator>();
		if (a != null)
		{
			a.SetBool("IsGrounded", true);
			a.enabled = value;
		}

	}


	/// <summary>
	/// performs a basic prediction algorithm that is a little bit
	/// more accurate than plain lerp
	/// </summary>
	protected virtual void UpdatePosition()
	{

		// wait if player has not yet been initialized
		if (Player == null)
			return;

		Init();

		// don't interpolate dead players (they should be handled by local ragdoll physics)
		if (Player.Dead.Active)
			return;

		// --- climbing ---
		if(Player.Climb.Active)
		{
			Transform.position = Vector3.Lerp(Transform.position, m_CurrentPosition, (m_StartedLerping ? Time.deltaTime * CLIMB_INTERPOLATION_SPEED : 1));
			return;
		}

		// --- platforms ---
		if (m_PlatformIDLastFrame != PlatformID)
		{
			// jumped onto, or off of, a platform
			m_LastPlatformPos = m_CurrentPosition;
			m_PlatformIDLastFrame = PlatformID;

			return;
		}
		else if(Player.Platform.Get() != null)
		{
			// standing on a platform
			m_CurrentPosition = Vector3.Lerp(m_LastPlatformPos, m_CurrentPosition, (m_StartedLerping ? Time.deltaTime * PLATFORM_INTERPOLATION_SPEED : 1));
			Transform.position = Player.Platform.Get().TransformPoint(m_CurrentPosition);
			return;
		}
		m_PlatformIDLastFrame = PlatformID;

		// --- prediction and interpolation ---

		// optionally, perform positional movement prediction for a more accurate
		// position overall, and especially during lagged conditions. we do this
		// by adding last incoming velocity to current position every frame
		if (PredictPosition)    // do not attempt prediction on ragdolls
								//&& Player.Platform.Get() == null)				// no prediction while on a platform
			Transform.position += (m_CurrentVelocity * Time.deltaTime);
		
		// always interpolate current position with last incoming position. this
		// makes movement smooth and has the character slide gently to the exact
		// network position after stopping
		Transform.position = Vector3.Lerp(Transform.position, m_CurrentPosition,
			(m_StartedLerping ?
				(Time.deltaTime * (PredictPosition ? 1.0f : 5.0f))	// stronger lerp with no prediction
				: 1));

		// --- snap cases ---

		StoreGroundAltitude();

		// prevent remote players from lerp-sliding over long distances
		if ((MaxDeviation.x > 0.0f) && Vector3.Distance(m_LastPosition, Transform.position) > MaxDeviation.x)
			SetPosition(m_LastPosition);

		// prevent body from ever sinking below ground, and from lerp-lagging
		// too much behind the latest incoming network altitude
		Transform.position = vp_3DUtility.HorizontalVector(Transform.position) +
							(Vector3.up * Mathf.Clamp(Transform.position.y, m_GroundAltitude,
							((MaxDeviation.y > 0) ? (m_CurrentPosition.y + MaxDeviation.y) : m_CurrentPosition.y)));

		// smooth-snap position to ground if falling while close to ground
		if ((m_Velocity.y < 0) && (Transform.position.y < m_GroundAltitude + GroundSnapRange))
			Transform.position = Vector3.Lerp(Transform.position,
				vp_3DUtility.HorizontalVector(Transform.position) + (Vector3.up * (m_GroundAltitude + Controller.SkinWidth)),
				Time.deltaTime * 20.0f);

	}


	/// <summary>
	/// handles on-join position and animation
	/// </summary>
	void Init()
	{

		if (!m_StartedLerping)
		{
			SetPosition(LastMasterPosition, LastMasterRotation);
			if (Player.Platform.Get() != null)
				SetPosition(Vector3.zero);
		}
		
		if (!m_IsAnimated)
			SetAnimated(true);

	}


	/// <summary>
	/// stores an interpolated velocity value for smooth animations
	/// </summary>
	protected virtual void UpdateVelocity()
	{

		if (Player == null)
			return;

		if (Player.Platform.Get() != null)
			Player.Velocity.Set((Player.InputMoveVector.Get().x * Vector3.left) + (Player.InputMoveVector.Get().y * Vector3.forward));
		else
			Player.Velocity.Set(Vector3.Lerp(Player.Velocity.Get(), m_CurrentVelocity, Time.deltaTime * 10.0f));
		
	}


	/// <summary>
	/// stores smooth rotation values based on last known rotation
	/// </summary>
	protected virtual void UpdateRotation()
	{

		// NOTES:

		// 1) we only need X (pitch) and Y (yaw). this class only uses the Y
		// value (for setting root transform yaw). the animator later fetches
		// X and Y via the 'Rotation' event (for purposes of  rotating the head
		// and spine bones). body yaw (feet direction) is calculated in the
		// animator and is purely cosmetical (as in no effect on gameplay).

		// 2) prediction is not really necessary here, as it is OK for the
		// rotation to drift behind somewhat. movement direction is handled
		// by serialization and firing angle is handled via RPC's.

		// 'LerpAngle' is used for proper full-circle rotation.
		m_SmoothRotation.x = Mathf.LerpAngle(m_SmoothRotation.x, m_CurrentRotation.x, Time.deltaTime * 10.0f);
		m_SmoothRotation.y = Mathf.LerpAngle(m_SmoothRotation.y, m_CurrentRotation.y, Time.deltaTime * 10.0f);
		Transform.rotation = Quaternion.Euler(m_SmoothRotation.y * Vector3.up);		// rotate collider

	}


	/// <summary>
	/// 
	/// </summary>
	public override void FixedUpdate()
	{

		base.FixedUpdate();
		
	}


	/// <summary>
	/// updates position, rotation and velocity over the network
	/// </summary>
	public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
		
		if (stream.isWriting)
			return;

		if (Player == null)
			return;

		PlatformID = (int)stream.ReceiveNext();
		m_LastPosition = (Vector3)stream.ReceiveNext();
		m_LastRotation = (Vector2)stream.ReceiveNext();
		m_LastVelocity = ((Vector3)stream.ReceiveNext());
		m_InputMoveVector = ((Vector2)stream.ReceiveNext());

		// DEBUG: uncomment the below line in BOTH vp_MPRemotePlayer AND vp_MPLocalPlayer ->
		// OnPhotonSerializeView to make the game detect while weapon index reported
		// by a remote machine goes out of sync with the one wielded on the corresponding
		// remote player on this machine - and to fix it
		ForceSyncWeapon(stream);

	}


	/// <summary>
	/// updates weapon over the network
	/// </summary>	
	protected override void ForceSyncWeapon(PhotonStream stream)
	{

		m_RemoteWeaponIndex = (int)stream.ReceiveNext();

		if (m_RemoteWeaponIndex == Player.CurrentWeaponIndex.Get())
			return;

		if (Player.SetWeapon.Active
			&& (Player.SetWeapon.Argument != null)
			&& ((int)Player.SetWeapon.Argument == m_RemoteWeaponIndex))
			return;

		if ((m_RemoteWeaponIndex == -1) && (Player.CurrentWeaponIndex.Get() == 0))
			return;

		if ((m_RemoteWeaponIndex == 0) && (Player.CurrentWeaponIndex.Get() == -1))
			return;

		// if we end up here we're out of sync. see if we can fix it
		Player.SetWeapon.TryStart(m_RemoteWeaponIndex);

		//Debug.LogError("Error (" + this + ") Weapon index was out of sync with remote machine. We had "
		//				+ Player.CurrentWeaponIndex.Get().ToString()
		//				+ " but remote machine says: "
		//				+ m_RemoteWeaponIndex
		//				);

	}


	/// <summary>
	/// refreshes common components along with components that are
	/// specific to the remote player
	/// </summary>
	protected override void RefreshComponents()
	{

		base.RefreshComponents();

		// refresh nametag team color
		if (NameTag != null)
		{
			if (vp_MPTeamManager.Exists && (TeamNumber > 0))
				NameTag.Color = vp_MPTeamManager.Instance.Teams[TeamNumber].Color;
			else
				NameTag.Color = Color.white;
		}

	}


	/// <summary>
	/// this is called from the base class and used to prevent lerp
	/// movement upon teleport. local player does not override it
	/// </summary>
	public override void SetPosition(Vector3 position, Quaternion rotation)
	{

		SetPosition(position);
		SetRotation(rotation);

	}


	/// <summary>
	/// this is called from the base class and used to prevent lerp
	/// rotation upon teleport. local player does not override it
	/// </summary>
	public override void SetRotation(Quaternion rotation)
	{

		if (Player == null)
			return;

		Player.Rotation.Set(rotation.eulerAngles);
		m_LastRotation = rotation.eulerAngles;
		m_CurrentRotation = rotation.eulerAngles;

	}


	/// <summary>
	/// this is called from the base class and used to prevent lerp
	/// movement upon teleport. local player does not override it
	/// </summary>
	public override void SetPosition(Vector3 position)
	{

		if (Player.Platform.Get() != null)
			position = Vector3.zero;

		Transform.position = position;
		m_LastPosition = position;
		m_CurrentPosition = position;

	}


	/// <summary>
	/// this is called from the base class and used to prevent lerp
	/// movement upon teleport. local player does not override it
	/// </summary>
	public virtual void SnapPosition()
	{

		SetPosition(m_LastPosition);
		SetRotation(Quaternion.Euler(m_LastRotation));

	}
	

	/// <summary>
	/// this method fires the remote player's weapon in a controlled
	/// manner in order to prevent firing rate hacks. basically, if
	/// you hack the client to fire 30 shots from a certain position
	/// in one frame, your next 30 shots will be fired from that
	/// position on remote machines - but at the 'legal' firing rate
	/// only and over the course of the next few seconds, regardless
	/// of your current position
	/// </summary>
	protected virtual void UpdateFiring()
	{

		// only do something if we have shots cueued up
		if (FireEvents.Count < 1)
			return;

		if (Player == null)
			return;

		if (WeaponHandler == null)
			return;

		// only fire shots that match the current weapon (if they don't,
		// we forget about them)
		if (WeaponHandler.CurrentWeaponIndex != FireEvents[0].WeaponIndex)
		{
			FireEvents.Remove(FireEvents[0]);
			return;
		}

		// TODO: fail if we have moved too far away from the fire event's position

		// try to fire the current weapon with this shot. if we fail, we try again
		// next frame. this is what enforces a player's firing rate on all machines
		if (!Player.Fire.Try())
			return;

		// if we succeed in firing a shot - forget about it
		FireEvents.Remove(FireEvents[0]);

	}
	

	/// <summary>
	/// returns the shooter of the passed fire event
	/// </summary>
	protected virtual vp_WeaponShooter GetShooterOfFireEvent(FireEvent fireEvent)
	{

		vp_WeaponShooter shooter;
		//Debug.Log("getting shooter with index: " + fireEvent.WeaponIndex);

		if (m_Shooters.TryGetValue(fireEvent.WeaponIndex, out shooter))
			return shooter;

		return null;

	}


	/// <summary>
	/// adjusts fire position to prevent remote player from shooting
	/// itself under lagged conditions
	/// </summary>
	protected virtual void AdjustForBodyCollider(FireEvent fireEvent)
	{

		if (m_BulletAdjusterTransform == null)
			return;

		if (fireEvent == null)
			return;

		vp_WeaponShooter shooter = GetShooterOfFireEvent(fireEvent);

		if (shooter == null)
			return;
		
		m_BulletAdjusterTransform.rotation = fireEvent.Rotation;
		shooter.SetSpread(fireEvent.Seed, m_BulletAdjusterTransform);

		// TODO: range should be ~ distance moved since last frame (?)
	//if (Physics.Linecast(fireEvent.Position, fireEvent.Position + (bulletAdjusterTransform.forward * 1000), out bulletAdjusterHit, vp_Layer.Mask.BulletBlockers))
	retry:
		if (Physics.Raycast(fireEvent.Position, m_BulletAdjusterTransform.forward, out m_BulletAdjusterHit, 1000, vp_Layer.Mask.IgnoreWalkThru))
		{
			if (m_BulletAdjusterHit.collider.transform.root == Transform.root/*m_BulletAdjusterHitcollider == BodyTransform.collider*/)	// TODO: cache body collider
			{
				//vp_MPDebug.Log("Hit myself @ " + Time.time);
				fireEvent.Position = m_BulletAdjusterHit.point + (m_BulletAdjusterTransform.forward * 0.01f); 
				goto retry;
			}

		}

	}

	
	/// <summary>
	/// iterates the 'Shots' variable by one (in the base class).
	/// also, creates a new fire event and adjusts it for our own body
	/// collider. for more info, see the summaries of 'UpdateFiring' in
	/// this class and 'FireWeapon' in vp_MPNetworkPlayer
	/// </summary>
	[PunRPC]
	public override void FireWeapon(int weaponIndex, Vector3 position, Quaternion rotation, PhotonMessageInfo info)
	{
		//Debug.Log("Got RPC 'FireWeapon', weapon: " + weaponIndex + ", position: " + position + ", rotation: " + rotation);

		base.FireWeapon(weaponIndex, position, rotation, info);

		// TODO: limit how often this can be called to prevent flooding
		// TODO: limit allowed distance from firing player (might cap distance back in direction towards remote player head if too far)

		FireEvent fireEvent = new FireEvent(weaponIndex, position, rotation, Shots);

		AdjustForBodyCollider(fireEvent);

		FireEvents.Add(fireEvent);

	}


	/// <summary>
	/// when this RPC arrives the player will die immediately because the
	/// master client says so. since this is a remote player, the nametag
	/// will fade out in one sec
	/// </summary>
	[PunRPC]
	public override void ReceivePlayerKill(PhotonMessageInfo info)
	{

		//Debug.Log(this + "ReceivePlayerKill");

		if (info.sender != PhotonNetwork.masterClient)
			return;

		if (NameTag != null)
		{
			vp_Timer.In(1, () =>
			{
				if ((this != null) && (NameTag != null))
					NameTag.Visible = false;
			});
		}

		base.ReceivePlayerKill(info);

	}


	/// <summary>
	/// when this RPC arrives the player will be instantly teleported
	/// to the position dictated by the master client and have its
	/// nametag instantly but temporarily made invisible
	/// </summary>
	[PunRPC]
	public override void ReceivePlayerRespawn(Vector3 position, Quaternion rotation, PhotonMessageInfo info)
	{

		//Debug.Log(this.GetType() + ".ReceivePlayerRespawn");

		base.ReceivePlayerRespawn(position, rotation, info);

		// if this is a remote player, make our nametag temporarily invisible on
		// respawn (or we might reveal our respawn position) then fade back in
		if (NameTag != null)
		{
			NameTag.Alpha = 0.0f;	// snap to invisible
			NameTag.Visible = true;	// fade back in
		}

	}


	// --- UFPS player events, received as RPCs and forwarded to our player event handler ---

	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Start_Crouch(PhotonMessageInfo info)
	{
		Player.Crouch.Start();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Stop_Crouch(PhotonMessageInfo info)
	{
		Player.Crouch.Stop();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Start_Run(PhotonMessageInfo info)
	{
		Player.Run.Start();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Stop_Run(PhotonMessageInfo info)
	{
		Player.Run.Stop();
	}

	
	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Start_SetWeapon(int weapon, PhotonMessageInfo info)
	{
		Player.SetWeapon.TryStart(weapon);
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Start_Attack(PhotonMessageInfo info)
	{
		Player.Attack.Start();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Stop_Attack(PhotonMessageInfo info)
	{
		Player.Attack.Stop();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Start_Zoom(PhotonMessageInfo info)
	{
		Player.Zoom.Start();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Stop_Zoom(PhotonMessageInfo info)
	{
		Player.Zoom.Stop();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Start_Reload(PhotonMessageInfo info)
	{
		Player.Reload.Start();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Stop_Reload(PhotonMessageInfo info)
	{
		Player.Reload.Stop();
	}

	
	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Start_Climb(PhotonMessageInfo info)
	{
		Player.Climb.Start();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Stop_Climb(PhotonMessageInfo info)
	{
		Player.Climb.Stop();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Start_OutOfControl(PhotonMessageInfo info)
	{
		Player.OutOfControl.Start();
	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	void Stop_OutOfControl(PhotonMessageInfo info)
	{
		Player.OutOfControl.Stop();
	}

	// --- locally originating player events ---

	/// <summary>
	/// gets or sets the rotation of the camera
	/// </summary>
	protected virtual Vector2 OnValue_Rotation
	{
		get
		{
			return m_SmoothRotation;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	Vector3 OnValue_Velocity
	{
		get
		{
			return m_Velocity;
		}
		set
		{
			m_Velocity = value;
		}
	}
	Vector3 m_Velocity = Vector3.zero;


	/// <summary>
	/// 
	/// </summary>
	Vector2 OnValue_InputMoveVector
	{
		get
		{
			return m_InputMoveVector;
		}
		set
		{
			m_InputMoveVector = value;
		}
	}
	Vector2 m_InputMoveVector = Vector2.zero;


	/// <summary>
	/// 
	/// </summary>
	void OnStart_Climb()
	{

		SnapPosition();

	}


	/// <summary>
	/// 
	/// </summary>
	void OnStop_Climb()
	{

		SnapPosition();

	}


}