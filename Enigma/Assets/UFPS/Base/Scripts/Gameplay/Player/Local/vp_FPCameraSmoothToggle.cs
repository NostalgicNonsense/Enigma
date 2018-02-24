/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPCameraSmoothToggle.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	put this script on a FPCamera gameobject to allow temporarily
//					passing control to another camera system (for example a cutscene)
//					and later to smoothly move the camera back to its FPS position
//					without popping
//
//					USAGE:
//
//					- the 'DisableFPCamera' method will disable the camera, spring and 
//					bob systems, store the original angle and current weapon, and
//					unwield the current weapon
//
//					- calling the 'EnableFPCamera' method will cause the script to
//					calculate the correct return target position every frame (taking
//					up-to-date controller position into consideration to avoid popping).
//					by default the 'InterpolateMethod' delegate will move the camera
//					smoothly back to the FPS player. when target is reached the FPCamera
//					will be reenabled and the weapon rewielded
//
//					- the 'InterpolateMethod' delegate can be set to null or some custom
//					logic that you may wish to use instead of linear interpolation. note
//					that whatever custom logic you use should move towards the 'TargetPos'
//					position, or there may be popping
//
//					- you may or may not need to put this script last in script execution
//						order to get rid of popping 100%
//
//					- uncomment the snippet at the top of 'LateUpdate' to make a quick test.
//					instructions:
//						1) after enabling the snippet, press 'H' in play mode to disable
//							the vp_FPCamera
//						2) move and rotate the camera to a different position in an editor
//							scene view
//						3) press 'H' again to watch it slide back to the player, reenabling
//							player control
//
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;

public class vp_FPCameraSmoothToggle : MonoBehaviour
{

	public Vector3 TargetPos;
	[Range(0, 30)]
	public float InterpolationSpeed = 10.0f;
	[Range(0, 0.1f)]
	public float RepositionSnap = 0.001f;	// 1 millimeter
	public float RerotateSnap = 0.1f;		// 1 tenth of a degree

	protected int m_WieldedWeapon = 0;
	protected Vector3 m_OriginalAngle = Vector3.zero;
	protected bool m_Restoring = false;

	public System.Action InterpolateMethod = null;

	vp_FPCamera m_FPCamera = null;
	vp_FPCamera FPCamera
	{
		get
		{
			if (m_FPCamera == null)
				m_FPCamera = transform.GetComponent<vp_FPCamera>();

			return m_FPCamera;
		}
	}

	vp_FPController m_FPController = null;
	vp_FPController FPController
	{
		get
		{
			if (m_FPController == null)
				m_FPController = GameObject.FindObjectOfType<vp_FPController>();
			return m_FPController;
		}
	}

	vp_FPPlayerEventHandler m_FPPlayer = null;
	vp_FPPlayerEventHandler FPPlayer
	{
		get
		{
			if (m_FPPlayer == null)
				m_FPPlayer = GameObject.FindObjectOfType<vp_FPPlayerEventHandler>();
			return m_FPPlayer;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected void Start()
	{

		if (FPCamera == null)
			Debug.LogError("Error (" + this + ") This script must sit on the same gameobject as a vp_FPCamera component.");

		InterpolateMethod = delegate
		{
			FPCamera.transform.position = Vector3.Lerp(FPCamera.transform.position, TargetPos, Time.deltaTime * InterpolationSpeed);

			FPCamera.transform.eulerAngles = new Vector3(
				Mathf.LerpAngle(FPCamera.transform.eulerAngles.x, m_OriginalAngle.x, Time.deltaTime * InterpolationSpeed),
				Mathf.LerpAngle(FPCamera.transform.eulerAngles.y, m_OriginalAngle.y, Time.deltaTime * InterpolationSpeed),
				0
			);
		};

	}
	

	/// <summary>
	/// disables a vp_FPCamera and its spring and bob system after
	/// backing up its original angle, unwielding the current weapon,
	/// stopping the controller and preventing gameplay input
	/// </summary>
	public void DisableFPCamera()
	{

		m_OriginalAngle = FPCamera.transform.eulerAngles;	// backup camera angle
		// NOTE: we can't backup camera position because controller could have moved

		m_WieldedWeapon = FPPlayer.CurrentWeaponIndex.Get();	// remember the weapon we are holding
		FPPlayer.SetWeapon.TryStart(0);	// unwield current weapon
		FPController.Stop();	// stop controller
		FPPlayer.InputAllowGameplay.Set(false);	// prevent player from moving
		FPCamera.SnapSprings();	// reset spring state to default
		FPCamera.StopSprings();	// stop springs and zero out bob
		FPCamera.enabled = false;	// disable vp_FPCamera
		
	}
	

	/// <summary>
	/// causes the FPS camera to reclaim control over the unity main
	/// camera using smooth interpolation to the original angle and
	/// current vp_FPController smooth position
	/// </summary>
	public void EnableFPCamera()
	{
		m_Restoring = true;
	}


	/// <summary>
	/// calculates correct target position for restoring the camera,
	/// optionally moves there after calling 'EnableCamera' and re-enables
	/// the camera when done
	/// </summary>
	protected void LateUpdate()
	{

		// SNIPPET: for testing this script in the editor. see top of file for instructions
		//if (Input.GetKeyUp(KeyCode.H))
		//{
		//	if (FPCamera.enabled)
		//		DisableFPCamera();
		//	else
		//		EnableFPCamera();
		//}

		if (!m_Restoring)
			return;

		// calculate target camera position depending on up-to-date controller position
		TargetPos = FPController.SmoothPosition + FPController.Transform.TransformDirection(FPCamera.PositionOffset);

		// interpolate position towards current target, and rotation towards original one
		InterpolateMethod.Invoke();

		// re-enable camera when we're within 1 millimeter from target position,
		// and within a tenth of a degree from original angle
		if (Vector3.Distance(FPCamera.transform.position, TargetPos) <= RepositionSnap
			&& (Mathf.DeltaAngle(FPCamera.transform.eulerAngles.x, m_OriginalAngle.x) < RerotateSnap)
			&& (Mathf.DeltaAngle(FPCamera.transform.eulerAngles.y, m_OriginalAngle.y) < RerotateSnap)
			)
		{
			FPPlayer.SetWeapon.TryStart(m_WieldedWeapon);	// rewield the weapon we held (if any)
			FPPlayer.InputAllowGameplay.Set(true);		// allow player to move
			FPCamera.enabled = true;	// reenable vp_FPCamera
			m_Restoring = false;
		}
		
	}


}
