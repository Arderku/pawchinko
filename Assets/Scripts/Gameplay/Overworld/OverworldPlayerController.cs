using UnityEngine;
using UnityEngine.InputSystem;

namespace Pawchinko
{
    /// <summary>
    /// Top-down overworld avatar movement, sprinting, facing, and animator speed updates.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class OverworldPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float turnSpeed = 12f;

        [Header("References")]
        [SerializeField] private Transform model;
        [SerializeField] private Animator animator;

        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;

        private CharacterController _controller;
        private bool _inputEnabled = true;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            SetActionsEnabled(_inputEnabled);
        }

        private void OnDisable()
        {
            SetActionsEnabled(false);
        }

        private void Update()
        {
            if (!_inputEnabled)
            {
                SetAnimatorSpeed(0f);
                return;
            }

            Vector2 input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            bool isSprinting = sprintAction != null && sprintAction.action.IsPressed();

            Vector3 move = new Vector3(input.x, 0f, input.y);
            float inputAmount = Mathf.Clamp01(move.magnitude);

            if (inputAmount > 0.01f)
            {
                float speed = isSprinting ? runSpeed : walkSpeed;
                _controller.Move(move.normalized * speed * Time.deltaTime);

                if (model != null)
                {
                    Quaternion targetRot = Quaternion.LookRotation(move);
                    model.rotation = Quaternion.Slerp(model.rotation, targetRot, turnSpeed * Time.deltaTime);
                }
            }

            float animSpeed = isSprinting ? 1f : 0.5f;
            SetAnimatorSpeed(inputAmount * animSpeed);
        }

        /// <summary>
        /// Enables or disables player input without disabling the whole GameObject.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            SetActionsEnabled(enabled && isActiveAndEnabled);
            if (!enabled)
            {
                SetAnimatorSpeed(0f);
            }
        }

        private void SetActionsEnabled(bool enabled)
        {
            SetActionEnabled(moveAction, enabled);
            SetActionEnabled(sprintAction, enabled);
        }

        private void SetActionEnabled(InputActionReference actionReference, bool enabled)
        {
            if (actionReference == null || actionReference.action == null) return;
            if (enabled) actionReference.action.Enable();
            else actionReference.action.Disable();
        }

        private void SetAnimatorSpeed(float speed)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", speed);
            }
        }
    }
}
