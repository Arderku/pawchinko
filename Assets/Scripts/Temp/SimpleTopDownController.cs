using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleTopDownController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public Transform model;

    [Header("Input")]
    public InputActionReference moveAction;

    private CharacterController controller;
    private Vector2 moveInput;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        moveAction.action.Enable();
    }

    void OnDisable()
    {
        moveAction.action.Disable();
    }

    void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        controller.Move(move * moveSpeed * Time.deltaTime);

        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            model.rotation = Quaternion.Slerp(
                model.rotation,
                targetRot,
                12f * Time.deltaTime
            );
        }
    }
}