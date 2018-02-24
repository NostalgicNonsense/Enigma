/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Layer.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this class defines the layers used on objects. you may want
//					to modify the integers assigned here to suit your needs.
//					for example, you may want to keep 'LocalPlayer' in another
//					layer or you may want to address rendering or physics issues
//					related to incompatibility with other systems
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public sealed class vp_Layer
{

	public static readonly vp_Layer instance = new vp_Layer();

	// built-in Unity layers
	public const int Default = 0;
	public const int TransparentFX = 1;
	public const int IgnoreRaycast = 2;
	public const int Water = 4;

	// standard layers
	public const int MovableObject = 21;	// used on movable physics objects to prevent spawning inside them
	public const int Ragdoll = 22;			// used to prevent collision between controller capsule and its ragdoll bodyparts
	public const int RemotePlayer = 23;		// used to distinguish the local player from multiplayer remote ones
	public const int IgnoreBullets = 24;	// use on objects with very crude colliders to avoid floating bullet decals
	public const int Enemy = 25;			// not used. please don't use. will soon be removed
	public const int Pickup = 26;			// used to filter out pickups on collision 
	public const int Trigger = 27;			// used to prevent bullets from hitting triggers
	public const int MovingPlatform = 28;	// used to make the player snap to, rotate with and inherit speed from moving platforms such as elevators
	public const int Debris = 29;			// used to filter out small flying rubble on collision 
	public const int LocalPlayer = 30;		// used for weaponcamera rendering, and to distinguish the local player from multiplayer remote ones
	public const int Weapon = 31;			// used for weaponcamera rendering

	public static class Mask
	{

		// layer mask for raycasting away from the local player, ignoring the player itself
		// and all non-solid objects, including rigidbody pickups (used for bullets)
		public static int BulletBlockers = ~((1 << LocalPlayer) | (1 << Debris) |
								(1 << IgnoreRaycast) | (1 << IgnoreBullets) | (1 << Trigger) | (1 << Water) | (1 << Pickup));

		// layer mask for raycasting away from the local player, ignoring the player itself
		// and all non-solid objects. (used for player physics)
		public static int ExternalBlockers = ~((1 << LocalPlayer) | (1 << Debris) |
								(1 << IgnoreRaycast) | (1 << Trigger) | (1 << RemotePlayer) | (1 << Ragdoll) | (1 << Water));

		// layer mask for detecting solid, moving objects. (used for spawn radius checking)
		public static int PhysicsBlockers = (1 << vp_Layer.LocalPlayer) | (1 << vp_Layer.MovingPlatform) | (1 << vp_Layer.MovableObject);

		// layer mask for filtering out small and walk-thru objects. (used for explosions)
		public static int IgnoreWalkThru = ~((1 << Debris) | (1 << IgnoreRaycast) |
								(1 << IgnoreBullets) | (1 << Trigger) | (1 << Water) | (1 << Pickup));
		 
	}


	/// <summary>
	///
	/// </summary>
	static vp_Layer()
	{
		Physics.IgnoreLayerCollision(LocalPlayer, Debris);		// player should never collide with small debris
		Physics.IgnoreLayerCollision(Debris, Debris);			// gun shells should not collide against each other
		Physics.IgnoreLayerCollision(Ragdoll, RemotePlayer);
	}
	private vp_Layer(){}


	/// <summary>
	/// sets the layer of a gameobject and optionally its descendants
	/// </summary>
	public static void Set(GameObject obj, int layer, bool recursive = false)
	{

		if (layer < 0 || layer > 31)
		{
			Debug.LogError("vp_Layer: Attempted to set layer id out of range [0-31].");
			return;
		}

		obj.layer = layer;

		if (recursive)
		{
			foreach (Transform t in obj.transform)
			{
				Set(t.gameObject, layer, true);
			}
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public static bool IsInMask(int layer, int layerMask)
	{
		return (layerMask & 1<<layer) == 0;
	}

}

