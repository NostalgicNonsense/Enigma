//----------------------------------------------
//            Realistic Tank Controller
//
// Copyright © 2014 - 2017 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

[AddComponentMenu("BoneCracker Games/Realistic Tank Controller/UI/Joystick")]
public class RTC_UIJoystickController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler {

	// Getting an Instance of Main Shared RTC Settings.
	#region RTC Settings Instance

	private RTC_Settings RTCSettingsInstance;
	private RTC_Settings RTCSettings {
		get {
			if (RTCSettingsInstance == null) {
				RTCSettingsInstance = RTC_Settings.Instance;
			}
			return RTCSettingsInstance;
		}
	}

	#endregion

	public enum AxisOption
	{
		// Options for which axes to use
		OnlyVertical, // Only vertical
		OnlyHorizontal, // Only horizontal
		Both // Use both
	}

	public int MovementRange = 100;
	public AxisOption axesToUse = AxisOption.Both; // The options for the axes that the still will use.

	public JoystickType joystickType;
	public enum JoystickType{Controlling, Aiming}

	internal float verticalInput = 0f;
	internal float horizontal = 0f;

	private float sensitivity{get{return RTCSettings.UIButtonSensitivity;}}
	private float gravity{get{return RTCSettings.UIButtonGravity;}}
	public bool pressing = false;

	Vector3 m_StartPos;

	void Start () {

		m_StartPos = transform.position;
	
	}

	void Update(){

		if (!pressing) {
			verticalInput = Mathf.MoveTowards (verticalInput, 0f, Time.deltaTime * gravity);
			horizontal = Mathf.MoveTowards (horizontal, 0f, Time.deltaTime * gravity);
		}

		if(verticalInput < -1f)
			verticalInput = -1f;
		if(verticalInput > 1f)
			verticalInput = 1f;

		if(horizontal < -1f)
			horizontal = -1f;
		if(horizontal > 1f)
			horizontal = 1f;

	}
	
	void UpdateVirtualAxes(Vector3 value){
		
		var delta = m_StartPos - value;
		delta.y = -delta.y;
		delta /= MovementRange;

		switch (axesToUse) {

		case AxisOption.OnlyVertical:
			verticalInput = delta.y;
			break;

		case AxisOption.OnlyHorizontal:
			horizontal = -delta.x;
			break;

		case AxisOption.Both:
			horizontal = -delta.x;
			verticalInput = delta.y;
			break;

		}

	}

	public void OnDrag(PointerEventData data){
		
		Vector3 newPos = Vector3.zero;

		int verticalDelta = 0;
		int horizontalDelta = 0;

		switch (axesToUse) {

		case AxisOption.OnlyVertical:
			verticalDelta = (int)(data.position.y - m_StartPos.y);
			verticalDelta = Mathf.Clamp(verticalDelta, -MovementRange, MovementRange);
			newPos.y = verticalDelta;
			break;

		case AxisOption.OnlyHorizontal:
			horizontalDelta = (int)(data.position.x - m_StartPos.x);
			horizontalDelta = Mathf.Clamp(horizontalDelta, - MovementRange, MovementRange);
			newPos.x = horizontalDelta;
			break;

		case AxisOption.Both:
			verticalDelta = (int)(data.position.y - m_StartPos.y);
			verticalDelta = Mathf.Clamp(verticalDelta, -MovementRange, MovementRange);
			newPos.y = verticalDelta;
			horizontalDelta = (int)(data.position.x - m_StartPos.x);
			horizontalDelta = Mathf.Clamp(horizontalDelta, - MovementRange, MovementRange);
			newPos.x = horizontalDelta;
			break;

		}

		transform.position = new Vector3(m_StartPos.x + newPos.x, m_StartPos.y + newPos.y, m_StartPos.z + newPos.z);
		UpdateVirtualAxes(transform.position);

	}


	public void OnPointerUp(PointerEventData data)	{

		pressing = false;
		transform.position = m_StartPos;
		UpdateVirtualAxes(m_StartPos);

	}


	public void OnPointerDown(PointerEventData data) {

		pressing = true;

	}

}
