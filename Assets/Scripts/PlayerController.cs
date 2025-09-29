using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Rigidbody))]

public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions playerControls;

    private InputAction moveAction;
    private Vector2 movementInput;

    [SerializeField] private GameObject player;
    private Rigidbody playerRB;

    [SerializeField] private float jumpForce = 50f;

    [SerializeField] private float baseSpeed = 2f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float accelerationTime = 3f; // Time to reach max speed
    private float currentSpeed = 0f;

    private float currentHoldTime = 0f;
    private Vector2 lastMoveValue = Vector2.zero;

    void Awake()
    {
        playerControls = new InputSystem_Actions();

        playerRB = gameObject.GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        playerControls.Enable();
        moveAction = playerControls.Player.Move;
        moveAction.Enable();
        playerControls.Player.Jump.performed += JumpAction;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        playerControls.Player.Jump.performed -= JumpAction;
        playerControls.Disable();
    }

    void Start()
    {
        //moveAction = InputSystem.actions.FindAction("move");
        //Vector2 moveValue = moveAction.ReadValue<Vector2>();
        //Debug.Log("Move Value: " + moveValue);
    }

    void Update()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        movementInput = moveValue;
        bool isButtonInput = IsButtonOrKeyInput();

        // Only apply acceleration for button/key inputs
        if (isButtonInput && moveValue.magnitude > 0.1f)
        {
            // Check if same direction is being held
            if (Vector2.Dot(moveValue.normalized, lastMoveValue.normalized) > 0.9f)
            {
                currentHoldTime += Time.deltaTime;
            }
            else
            {
                currentHoldTime = 0f; // Reset if direction changed
            }
        }
        else
        {
            currentHoldTime = 0f; // Reset if no input or analog input
        }

        // Calculate speed based on hold time (only for button inputs)
        currentSpeed = baseSpeed;
        if (isButtonInput && currentHoldTime > 0)
        {
            currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, currentHoldTime / accelerationTime);
        }

        lastMoveValue = moveValue;
    }
    void FixedUpdate()
    {
        //playerRB.MovePosition(playerRB.position +
        //    new Vector3(movementInput.x, 0, movementInput.y) * currentSpeed * Time.fixedDeltaTime);
        // Use velocity instead of MovePosition for proper collision detection
        Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y) * currentSpeed;
        playerRB.linearVelocity = new Vector3(movement.x, playerRB.linearVelocity.y, movement.z);
    }
    private void JumpAction(InputAction.CallbackContext context)
    {
        bool isGrounded = Physics.Raycast(player.transform.position, Vector3.down, 1.1f);
        if (context.performed && isGrounded)
        {
            //playerRB.AddForce(Vector3.up * 40, ForceMode.Impulse);
            playerRB.linearVelocity = new Vector3(playerRB.linearVelocity.x, 0, playerRB.linearVelocity.z);
            playerRB.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            // isGrounded = false;
        }
    }

    private bool IsButtonOrKeyInput()
    {
        var activeControl = moveAction.activeControl;
        if (activeControl == null) return false;

        // Check if it's a button or key control
        return activeControl is ButtonControl ||
               activeControl is KeyControl ||
               (activeControl.device is Keyboard);
    }
    private void QuickQuit()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit(); // For standalone builds
#endif
    }    
}
