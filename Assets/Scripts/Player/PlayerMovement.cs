using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vortex.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 15f;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }

        private Rigidbody _rigidbody;
        private Vector3 _moveInput;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Trava posição Y e rotações X e Z para estabilidade física no plano XZ
            _rigidbody.constraints = RigidbodyConstraints.FreezePositionY 
                                   | RigidbodyConstraints.FreezeRotationX 
                                   | RigidbodyConstraints.FreezeRotationZ;
        }

        private void Update()
        {
            Vector2 rawInput = ReadInput();
            _moveInput = new Vector3(rawInput.x, 0f, rawInput.y);

            if (_moveInput.sqrMagnitude > 1f)
            {
                _moveInput.Normalize();
            }
        }

        private void FixedUpdate()
        {
            Move();
            RotateTowardsMovement();
        }

        private Vector2 ReadInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                Vector2 dpad = Gamepad.current.dpad.ReadValue();

                if (stick.sqrMagnitude > input.sqrMagnitude) input = stick;
                if (dpad.sqrMagnitude > input.sqrMagnitude) input = dpad;
            }
#else
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
#endif

            return input;
        }

        private void Move()
        {
            Vector3 targetVelocity = new Vector3(_moveInput.x * moveSpeed, 0f, _moveInput.z * moveSpeed);

#if UNITY_6000_0_OR_NEWER
            _rigidbody.linearVelocity = targetVelocity;
#else
            _rigidbody.velocity = targetVelocity;
#endif
        }

        private void RotateTowardsMovement()
        {
            if (_moveInput.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_moveInput, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }
}
