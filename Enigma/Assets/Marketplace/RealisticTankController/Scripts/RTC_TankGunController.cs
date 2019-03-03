//----------------------------------------------
//            Realistic Tank Controller
//
// Copyright © 2014 - 2017 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

#pragma warning disable 0414

using UnityEngine;

#if RTC_REWIRED
using Rewired;
#endif

namespace Marketplace.RealisticTankController.Scripts
{
    [RequireComponent (typeof (Rigidbody))]
    [AddComponentMenu("BoneCracker Games/Realistic Tank Controller/Main/Gun Controller")]
    public class RTC_TankGunController : MonoBehaviour {

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

        private Rigidbody tankRigid;
        private Rigidbody mainGunRigid;
        private RTC_TankController tank;
        private RTC_MainCamera tankCamera;

        internal AimType aimType;
        public enum AimType{Orbit, Direct}

        [Range(0f, 1f)]public float horizontalSensitivity = .05f;
        [Range(0f, 1f)]public float verticalSensitivity = .15f;

        [Space()]
        public GameObject mainGun;

        public bool canControl = true;

        [Space()]
        public GameObject barrel;
        public Transform barrelOut;
	 
        private float steerInput = 0f;
        private float elevatorInput = 0f;

        public int rotationTorque = 1000;

        [Space()]
        public float maximumAngularVelocity = 1.5f;
        public int maximumRotationLimit = 160;
        public float minimumElevationLimit = 10;
        public float maximumElevationLimit = 25;

        internal float rotationVelocity;
        internal float rotationOfTheGun;

        internal Transform locatedTarget;
        internal Transform directedTarget;

        public int selectedAmmunation = 0;

        public GameObject projectile{

            get{
                return RTC_Ammunation.Instance.ammunations [selectedAmmunation].projectile;
            }

        }
        public int bulletVelocity{

            get{
                return RTC_Ammunation.Instance.ammunations [selectedAmmunation].velocity;
            }

        }
        public int recoilForce{

            get{
                return RTC_Ammunation.Instance.ammunations [selectedAmmunation].recoilForce;
            }

        }

        public int currentAmmo = 15;

        public float reloadTime{

            get{
                return RTC_Ammunation.Instance.ammunations [selectedAmmunation].reloadTime;
            }

        }

        private float loadingTime = 0f;

        public bool reloading{

            get{
                return loadingTime < reloadTime ? true : false;
            }

        }

        private AudioSource fireSoundSource;
        public AudioClip fireSoundClip{

            get{
                return RTC_Ammunation.Instance.ammunations [selectedAmmunation].fireSoundClip;
            }

        }

        public GameObject groundSmoke{

            get{
                return RTC_Ammunation.Instance.ammunations [selectedAmmunation].groundSmoke;
            }

        }
        public GameObject fireSmoke{

            get{
                return RTC_Ammunation.Instance.ammunations [selectedAmmunation].fireSmoke;
            }

        }

#if RTC_REWIRED
	private static Player player;
	#endif
		
        void Awake () {

            tankRigid = GetComponent<Rigidbody>();
            mainGunRigid = mainGun.GetComponent<Rigidbody>();
            tank = gameObject.GetComponent<RTC_TankController>();
            tankCamera = GameObject.FindObjectOfType<RTC_MainCamera>();

            GameObject newTarget = new GameObject("Located Target");
            locatedTarget = newTarget.transform;

            GameObject newTarget2 = new GameObject("Directed Target");
            directedTarget = newTarget2.transform;
	
            mainGunRigid.maxAngularVelocity = maximumAngularVelocity;
            mainGunRigid.interpolation = RigidbodyInterpolation.None;
            mainGunRigid.interpolation = RigidbodyInterpolation.Interpolate;

#if RTC_REWIRED
		player = Rewired.ReInput.players.GetPlayer(0);
		#endif

        }

        void Update(){

            if(!tank.canControl || !canControl)
                return;

            if (!tankCamera) {
                Debug.LogError ("RTC Tank Camera not found! Camera needed for aiming...");
                return;
            }

            loadingTime += Time.deltaTime;
            locatedTarget.position = tankCamera.cam.transform.position + (tankCamera.cam.transform.forward * 100f);
            directedTarget.position = barrelOut.transform.position + (barrelOut.transform.forward * 100f);

            Inputs ();

        }

        void Inputs(){

            switch(tankCamera.cameraMode){

                case RTC_MainCamera.CameraMode.FPS:
                    aimType = AimType.Direct;
                    break;

                case RTC_MainCamera.CameraMode.ORBIT:
                    aimType = AimType.Orbit;
                    break;

            }

            Vector3 targetPosition = mainGun.transform.InverseTransformPoint(locatedTarget.transform.position);
            Vector3 targetPosition2 = barrel.transform.InverseTransformPoint(locatedTarget.transform.position);

            switch (RTCSettings.controllerType) {

                case RTC_Settings.ControllerType.Keyboard:

                    switch (aimType) {

                        case AimType.Orbit:
                            steerInput = (targetPosition.x / targetPosition.magnitude);
                            elevatorInput = -(targetPosition2.y / targetPosition2.magnitude);
                            break;

                        case AimType.Direct:
                            steerInput = Mathf.Lerp(steerInput, Input.GetAxis (RTCSettings.mainGunXInput) * horizontalSensitivity, Time.deltaTime * 10f);
                            elevatorInput = Mathf.Lerp(elevatorInput, -Input.GetAxis (RTCSettings.mainGunYInput) * verticalSensitivity, Time.deltaTime * 10f);
                            break;

                    }

                    if (Input.GetKeyDown (RTCSettings.fireKB)) {
                        Fire ();
                    }

                    if(Input.GetKeyDown(RTCSettings.changeAmmunation)){
                        if (selectedAmmunation < RTC_Ammunation.Instance.ammunations.Length - 1)
                            selectedAmmunation ++;
                        else
                            selectedAmmunation = 0;
                    }

                    break;

                case RTC_Settings.ControllerType.Mobile:

                    switch (aimType) {

                        case AimType.Orbit:
                            steerInput = (targetPosition.x / targetPosition.magnitude);
                            elevatorInput = -(targetPosition2.y / targetPosition2.magnitude);
                            break;

                        case AimType.Direct:
                            steerInput = Mathf.Lerp(steerInput, RTC_UIMobileButtons.Instance.GetValues().aimingHorizontal * horizontalSensitivity, Time.deltaTime * 100f);
                            elevatorInput = Mathf.Lerp(elevatorInput, -RTC_UIMobileButtons.Instance.GetValues().aimingVertical * verticalSensitivity, Time.deltaTime * 100f);
                            break;

                    }

                    break;

                case RTC_Settings.ControllerType.Custom:

#if RTC_REWIRED
			switch (aimType) {

			case AimType.Orbit:
				steerInput = (targetPosition.x / targetPosition.magnitude);
				elevatorInput = -(targetPosition2.y / targetPosition2.magnitude);
				break;

			case AimType.Direct:
				steerInput = Mathf.Lerp(steerInput, player.GetAxis (RTCSettings.RW_mainGunXInput) * horizontalSensitivity, Time.deltaTime * 100f);
				elevatorInput = Mathf.Lerp(elevatorInput, -player.GetAxis (RTCSettings.RW_mainGunYInput) * verticalSensitivity, Time.deltaTime * 100f);
				break;

			}

			if (player.GetButtonDown (RTCSettings.RW_fireKB)) {
				Fire ();
			}

			if(player.GetButtonDown(RTCSettings.RW_changeAmmunation.ToString())){
				if (selectedAmmunation < RTC_Ammunation.Instance.ammunations.Length - 1)
					selectedAmmunation ++;
				else
					selectedAmmunation = 0;
			}
			#endif

                    break;

            }
			
        }

        void FixedUpdate () {

            if(!tank.canControl || !canControl)
                return;

            rotationVelocity = mainGunRigid.angularVelocity.y;

            if(mainGun.transform.localEulerAngles.y > 0 && mainGun.transform.localEulerAngles.y < 180)
                rotationOfTheGun = mainGun.transform.localEulerAngles.y;
            else
                rotationOfTheGun = mainGun.transform.localEulerAngles.y - 360;
	
            mainGunRigid.AddRelativeTorque(0, (rotationTorque) * steerInput, 0, ForceMode.Acceleration);
            barrel.transform.localRotation = barrel.transform.localRotation * Quaternion.AngleAxis(((elevatorInput) * 10f), Vector3.right);


            if (barrel) {

                if (barrel.transform.localEulerAngles.x > 0 && barrel.transform.localEulerAngles.x < 180)
                    barrel.transform.localRotation = Quaternion.Euler (new Vector3 (Mathf.Clamp (barrel.transform.localEulerAngles.x, -360, minimumElevationLimit), 0, 0));
                if (barrel.transform.localEulerAngles.x > 180 && barrel.transform.localEulerAngles.x < 360)
                    barrel.transform.localRotation = Quaternion.Euler (new Vector3 (Mathf.Clamp (barrel.transform.localEulerAngles.x - 360, -maximumElevationLimit, 360), 0, 0));

            }

        }

        public void Fire(){

            if (loadingTime < reloadTime || currentAmmo <= 0f)
                return;

            tankRigid.AddForce(-mainGun.transform.forward * recoilForce, ForceMode.Impulse);
            Rigidbody shot = Instantiate(projectile.GetComponent<Rigidbody>(), barrelOut.position, barrelOut.rotation) as Rigidbody;
            shot.AddForce(barrelOut.forward * bulletVelocity, ForceMode.VelocityChange);
            if(groundSmoke)
                Instantiate(groundSmoke, new Vector3(tank.transform.position.x, tank.transform.position.y, tank.transform.position.z), tank.transform.rotation);
            if(fireSmoke)
                Instantiate(fireSmoke, barrelOut.transform.position, barrelOut.transform.rotation);
            fireSoundSource = RTC_CreateAudioSource.NewAudioSource(gameObject, "FireSound", 30f, 500f, 1f, fireSoundClip, false, true, true);
            currentAmmo --;
            loadingTime = 0;

        }

        public void ChangeAmmunation(){

            if (selectedAmmunation < RTC_Ammunation.Instance.ammunations.Length - 1)
                selectedAmmunation ++;
            else
                selectedAmmunation = 0;

        }

        public void ChangeAmmunation(int ammunationIndex){

            if (ammunationIndex < RTC_Ammunation.Instance.ammunations.Length - 1)
                selectedAmmunation = ammunationIndex;

        }

    }
}
