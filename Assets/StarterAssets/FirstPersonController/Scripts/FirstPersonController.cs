using UnityEngine; // Unity engine core types
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // new Input System (only compiled when the package is active)
#endif

namespace StarterAssets
{
    /// <summary>
    /// First-person player controller: WASD movement, sprint, jump, gravity,
    /// and mouse/touch camera look. Drives a CharacterController component.
    /// Input values come from StarterAssetsInputs (keyboard/mouse on PC,
    /// MobileInputBridge on Android/iOS).
    /// </summary>
    [RequireComponent(typeof(CharacterController))] // this script needs a CharacterController on the same object
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]         // and a PlayerInput when using the new Input System
#endif
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

		// cinemachine
		private float _cinemachineTargetPitch;    // current up/down camera angle in degrees

		// player
		private float _speed;                     // current smoothed movement speed (m/s)
		private float _rotationVelocity;          // how fast we're yawing this frame (degrees)
		private float _verticalVelocity;          // current up/down speed from gravity/jumping (m/s)
		private float _terminalVelocity = 53.0f;  // max fall speed — real-world terminal velocity

		// timeout deltatime
		private float _jumpTimeoutDelta;          // countdown until the next jump is allowed
		private float _fallTimeoutDelta;          // countdown before entering "falling" state (helps on stairs)

#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;         // new Input System component (PC keyboard/mouse)
#endif
		private CharacterController _controller;  // the physics capsule that actually moves us
		private StarterAssetsInputs _input;       // shared store of current input values
		private GameObject _mainCamera;           // scene's main camera (found by tag)

		private const float _threshold = 0.01f;   // ignore look inputs smaller than this (anti-jitter)

		// true only when the active control scheme is mouse+keyboard
		private bool IsCurrentDeviceMouse
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false; // old Input Manager: no control-scheme concept
#endif
			}
		}

		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>(); // grab the physics capsule
			_input = GetComponent<StarterAssetsInputs>();      // grab the input store

			// if StarterAssetsInputs is missing, add it so input always exists
			if (_input == null)
			{
				_input = gameObject.AddComponent<StarterAssetsInputs>();
			}

#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
			if (_playerInput == null)
			{
				Debug.LogError("Starter Assets package is missing dependencies. PlayerInput not found. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
			}
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			// start the cooldown timers at their configured values
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
		}

		private void Update()
		{
			JumpAndGravity(); // handle jumping + apply gravity
			GroundedCheck();  // detect if we're standing on the ground
			Move();           // read input and move the character
		}

		private void LateUpdate()
		{
			CameraRotation(); // camera runs after movement so it follows the final position smoothly
		}

		private void GroundedCheck()
		{
			// position of an invisible sphere at the character's feet
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			// true if that sphere overlaps any ground-layer collider
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

        private void CameraRotation()
        {
            // if there is an input
            if (_input.look.sqrMagnitude >= _threshold)
            {
                // Don't multiply mouse input by Time.deltaTime.
                // Touch input (from TouchLook/MobileInputBridge) is already expressed
                // as per-frame degrees, so it also must not be scaled by deltaTime.
                float deltaTimeMultiplier = (IsCurrentDeviceMouse || Application.isMobilePlatform) ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                // clamp our pitch rotation
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                // Update Cinemachine camera target pitch
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

                // rotate the player left and right
                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void Move()
        {
            // pick the target speed: sprinting speed if sprint is held, else walk speed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // note: Vector2's == operator uses approximation so it isn't floating-point error prone
            // no input means we want to stop
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // how fast we're currently moving horizontally (ignoring vertical fall speed)
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f; // dead band so speed doesn't wobble around the target
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f; // analog stick = partial speed, keyboard = full

            // accelerate/decelerate toward the target if we're outside the dead band
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // Lerp gives a smooth, organic ease-in/out instead of instant speed changes
                // (T is clamped by Lerp, so the result can't overshoot)
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // round to 3 decimals to avoid tiny meaningless speed values
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed; // close enough: snap to the target
            }

            // convert the 2D input into a 3D direction (x = strafe, y input = forward)
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // if moving, rotate that direction by where the player is facing
            if (_input.move != Vector2.zero)
            {
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }

            // actually move the character: horizontal input + vertical gravity/jump velocity
            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
    }
}