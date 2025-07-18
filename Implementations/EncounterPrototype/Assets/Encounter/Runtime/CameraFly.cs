using UnityEngine;
using UnityEngine.InputSystem;

namespace Encounter.Runtime
{
    public class CameraFly : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private Vector2 lookSpeed = new(120f, 120f);
        [SerializeField] private Vector2 moveSpeed = new(100f, 100f);
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private float sprintSpeedMultiplier = 2f;

        [Header("Input Settings")]
        [SerializeField] private bool invertY;

        private Vector2 _lookInput;
        private Vector2 _moveInput;
        private Vector3 _currentVelocity;
        private Vector3 _targetVelocity;
        private float _rotationX;
        private float _rotationY;

        // Input action references
        private InputAction _lookAction;
        private InputAction _moveAction;
        private InputAction _sprintAction;

        private void Awake()
        {
            // Get input actions
            _lookAction = InputSystem.actions.FindAction("Look");
            _moveAction = InputSystem.actions.FindAction("Move");
            _sprintAction = InputSystem.actions.FindAction("Sprint");
        }

        private void Update()
        {
            HandleInput();
            HandleRotation();
            HandleMovement();
        }

        private void HandleInput()
        {
            // Get look input
            _lookInput = _lookAction.ReadValue<Vector2>();

            // Get movement input
            _moveInput = _moveAction.ReadValue<Vector2>();

            // Calculate target velocity based on input
            var forward = transform.forward * (_moveInput.y * moveSpeed.y);
            var right = transform.right * (_moveInput.x * moveSpeed.x);

            var sprint = _sprintAction.ReadValue<float>() > 0.0f;
            var speedMultiplier = sprint ? sprintSpeedMultiplier : 1f;
            _targetVelocity = (forward + right) * speedMultiplier;
        }

        private void HandleRotation()
        {
            // Apply look speed
            var deltaX = _lookInput.x * lookSpeed.x * Time.deltaTime;
            var deltaY = _lookInput.y * lookSpeed.y * Time.deltaTime;

            // Invert Y axis if needed (mouse Y is already inverted in screen space)
            if (!invertY)
            {
                deltaY = -deltaY;
            }

            // Update rotation values
            _rotationY += deltaX;
            _rotationX += deltaY;

            // Clamp vertical rotation
            _rotationX = Mathf.Clamp(_rotationX, -90f, 90f);

            // Apply rotation
            transform.localRotation = Quaternion.Euler(_rotationX, _rotationY, 0f);
        }

        private void HandleMovement()
        {
            // Apply smoothed movement
            transform.position += Vector3.SmoothDamp(
                Vector3.zero,
                _targetVelocity,
                ref _currentVelocity,
                smoothTime
            ) * Time.deltaTime;
        }
    }
}