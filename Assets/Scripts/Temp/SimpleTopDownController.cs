using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleTopDownController : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    public Transform model;
    public Animator animator; // expose this

    public InputActionReference moveAction;
    public InputActionReference sprintAction;

    private CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        moveAction.action.Enable();
        sprintAction.action.Enable();
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        sprintAction.action.Disable();
    }

    void Update()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        bool isSprinting = sprintAction.action.IsPressed();

        Vector3 move = new Vector3(input.x, 0f, input.y);
        float inputAmount = move.magnitude;

        float speed = isSprinting ? runSpeed : walkSpeed;
        controller.Move(move.normalized * speed * Time.deltaTime);

        float animSpeed = isSprinting ? 1f : 0.5f;
        animator.SetFloat("Speed", inputAmount * animSpeed);

        if (inputAmount > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            model.rotation = Quaternion.Slerp(model.rotation, targetRot, 12f * Time.deltaTime);
        }
    }
}