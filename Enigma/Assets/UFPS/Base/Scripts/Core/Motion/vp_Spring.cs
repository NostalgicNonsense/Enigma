/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Spring.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a simple but powerful spring logic for transform manipulation
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public class vp_Spring
{

	protected UpdateMode Mode = UpdateMode.Position;
	public enum UpdateMode
	{
		Position,
		PositionAdditiveLocal,	// adds position in relation to parent transform
		PositionAdditiveGlobal,	// adds position in relation to world
		PositionAdditiveSelf,	// adds position in relation to own transform
		Rotation,
		RotationAdditiveLocal,	// rotates in relation to parent transform
		RotationAdditiveGlobal,	// rotates in relation to world
		Scale,
		ScaleAdditiveLocal
	}

	protected bool m_AutoUpdate = true;

	// a delegate is used to modify the parent transform
	// with the current motion values
	protected delegate void UpdateDelegate();
	protected UpdateDelegate m_UpdateFunc;

	// the 'State' variable dictates either position, rotation or scale
	// of the gameobject, as defined by m_UpdateMode
	public Vector3 State = Vector3.zero;

	// the current velocity of the spring, as resulting from added forces
	protected Vector3 m_Velocity = Vector3.zero;

	// the static equilibrium of this spring
	public Vector3 RestState = Vector3.zero;

	// mechanical strength of the spring
	public Vector3 Stiffness = new Vector3(0.5f, 0.5f, 0.5f);

	// 'Damping' makes spring velocity wear off as it
	// approaches its rest state
	public Vector3 Damping = new Vector3(0.75f, 0.75f, 0.75f);

	// force velocity fadein variables
	protected float m_VelocityFadeInCap = 1.0f;
	protected float m_VelocityFadeInEndTime = 0.0f;
	protected float m_VelocityFadeInLength = 0.0f;

	// smooth force
	protected Vector3[] m_SoftForceFrame = new Vector3[120];
	
	// transform limitations
	public float MaxVelocity = 10000.0f;
	public float MinVelocity = 0.0000001f;
	public Vector3 MaxState = new Vector3(10000, 10000, 10000);
	public Vector3 MinState = new Vector3(-10000, -10000, -10000);

	// transform & property
	protected Transform m_Transform = null;
	public Transform Transform
	{
		get
		{
			return m_Transform;
		}
		set
		{
			m_Transform = value;
			RefreshUpdateMode();
		}
	}


	/// <summary>
	///
	/// </summary>
	public vp_Spring(Transform transform, UpdateMode mode, bool autoUpdate = true)
	{

		Mode = mode;
		Transform = transform;
		m_AutoUpdate = autoUpdate;

	}


	/// <summary>
	/// this should be called from a monobehaviour's FixedUpdate
	/// </summary>
	public void FixedUpdate()
	{

		// handle forced velocity fadein
		if (m_VelocityFadeInEndTime > Time.time)
			m_VelocityFadeInCap = Mathf.Clamp01(1 - ((m_VelocityFadeInEndTime - Time.time) / m_VelocityFadeInLength));
		else
			m_VelocityFadeInCap = 1.0f;

		// handle smooth force
		if (m_SoftForceFrame[0] != Vector3.zero)
		{
			AddForceInternal(m_SoftForceFrame[0]);
			for (int v = 0; v < 120; v++)
			{
				m_SoftForceFrame[v] = (v < 119) ? m_SoftForceFrame[v + 1] : Vector3.zero;
				if (m_SoftForceFrame[v] == Vector3.zero)
					break;
			}
		}

		// do the spring calculations
		Calculate();

		// update the transform state
		m_UpdateFunc();

	}


	///////////////////////////////////////////////////////////
	// update pointer functions. these apply the spring state
	// to the transform's local position, euler angles or scale
	// every fixed update
	///////////////////////////////////////////////////////////

	void Position() { m_Transform.localPosition = State; }
	void Rotation() {	m_Transform.localEulerAngles = State; }
	void Scale()	{	m_Transform.localScale = State; }


	///////////////////////////////////////////////////////////
	// additive update pointer functions. these are for using
	// a spring on an object which already has another spring
	// acting upon it (in order not to block out the motion
	// from the existing spring)
	///////////////////////////////////////////////////////////

	void PositionAdditiveLocal()	{	m_Transform.localPosition += State; }
	void PositionAdditiveGlobal()	{	m_Transform.position += State; }
	void PositionAdditiveSelf()		{	m_Transform.Translate(State, m_Transform); }
	void RotationAdditiveLocal()	{	m_Transform.localEulerAngles += State; }
	void RotationAdditiveGlobal()	{	m_Transform.eulerAngles += State; }
	void ScaleAdditiveLocal()		{	m_Transform.localScale += State; }


	///////////////////////////////////////////////////////////
	// empty pointer function. for cases when the transform needs
	// to be updated in a specific way by an external method
	///////////////////////////////////////////////////////////

	void None() { }


	/// <summary>
	/// this method sets the appropriate delegate for the trans-
	/// formation and syncs 'State' with the parent transform
	/// </summary>
	protected void RefreshUpdateMode()
	{

		m_UpdateFunc = None;

		switch (Mode)
		{
			case UpdateMode.Position:				State = m_Transform.localPosition;		if (m_AutoUpdate) m_UpdateFunc = Position;			break;
			case UpdateMode.Rotation:				State = m_Transform.localEulerAngles;	if (m_AutoUpdate) m_UpdateFunc = Rotation;			break;
			case UpdateMode.Scale:					State = m_Transform.localScale;			if (m_AutoUpdate) m_UpdateFunc = Scale;				break;
			case UpdateMode.PositionAdditiveLocal:	State = m_Transform.localPosition;		if (m_AutoUpdate) m_UpdateFunc = PositionAdditiveLocal; break;
			case UpdateMode.PositionAdditiveGlobal:	State = m_Transform.position;			if (m_AutoUpdate) m_UpdateFunc = PositionAdditiveGlobal; break;
			case UpdateMode.RotationAdditiveLocal:	State = m_Transform.localEulerAngles;	if (m_AutoUpdate) m_UpdateFunc = RotationAdditiveLocal; break;
			case UpdateMode.RotationAdditiveGlobal:	State = m_Transform.eulerAngles;		if (m_AutoUpdate) m_UpdateFunc = RotationAdditiveGlobal; break;
			case UpdateMode.PositionAdditiveSelf:	State = m_Transform.position;			if (m_AutoUpdate) m_UpdateFunc = PositionAdditiveSelf; break;
			case UpdateMode.ScaleAdditiveLocal:		State = m_Transform.localScale;			if (m_AutoUpdate) m_UpdateFunc = ScaleAdditiveLocal; break;
		}

		RestState = State;

	}


	/// <summary>
	/// performs the spring calculations
	/// </summary>
	protected void Calculate()
	{

		if (State == RestState)
			return;

		m_Velocity += Vector3.Scale((RestState - State), Stiffness);			// add rest state distance * stiffness to velocity
		m_Velocity = (Vector3.Scale(m_Velocity, Damping));					// dampen velocity

		// clamp velocity to maximum
		m_Velocity = Vector3.ClampMagnitude(m_Velocity, MaxVelocity);

		// apply velocity, or stop if velocity is below minimum
		if (m_Velocity.sqrMagnitude > (MinVelocity * MinVelocity))
			Move();
		else
			Reset();

	}


	/// <summary>
	/// adds external velocity to the spring in one frame
	/// </summary>
	private void AddForceInternal(Vector3 force)
	{

		force *= m_VelocityFadeInCap;
		m_Velocity += force;
		m_Velocity = Vector3.ClampMagnitude(m_Velocity, MaxVelocity);
		Move();

	}


	/// <summary>
	/// adds external velocity to the spring in one frame
	/// </summary>
	public void AddForce(Vector3 force)
	{

		if (Time.timeScale < 1.0f)
			AddSoftForce(force, 1);
		else
			AddForceInternal(force);

	}


	/// <summary>
	/// adds a force distributed over up to 120 fixed frames
	/// </summary>
	public void AddSoftForce(Vector3 force, float frames)
	{

		force /= Time.timeScale;

		frames = Mathf.Clamp(frames, 1, 120);

		AddForceInternal(force / frames);

		for (int v = 0; v < (Mathf.RoundToInt(frames)-1); v++)
		{
			m_SoftForceFrame[v] += (force / frames);
		}

	}


	/// <summary>
	/// adds velocity to the state and clamps state between min
	/// and max values
	/// </summary>
	protected void Move()
	{

		State += m_Velocity	* Time.timeScale;
		State.x = Mathf.Clamp(State.x, MinState.x, MaxState.x);
		State.y = Mathf.Clamp(State.y, MinState.y, MaxState.y);
		State.z = Mathf.Clamp(State.z, MinState.z, MaxState.z);

	}


	/// <summary>
	/// stops spring velocity and resets state to the static
	/// equilibrium
	/// </summary>
	public void Reset()
	{

		m_Velocity = Vector3.zero;
		State = RestState;

	}


	/// <summary>
	/// stops spring velocity
	/// </summary>
	public void Stop(bool includeSoftForce = false)
	{

		m_Velocity = Vector3.zero;
		if (includeSoftForce)
			StopSoftForce();

	}


	/// <summary>
	/// stops soft force
	/// </summary>
	public void StopSoftForce()
	{

		for (int v = 0; v < 120; v++)
		{
			m_SoftForceFrame[v] = Vector3.zero;
		}

	}


	/// <summary>
	/// instantly kills any forces added to the spring, gradually
	/// easing them back in over 'seconds'.
	/// this is useful when you need a spring to freeze up for a
	/// brief amount of time, then slowly relaxing back to normal.
	/// </summary>
	public void ForceVelocityFadeIn(float seconds)
	{

		m_VelocityFadeInLength = seconds;
		m_VelocityFadeInEndTime = Time.time + seconds;
		m_VelocityFadeInCap = 0.0f;

	}


}

