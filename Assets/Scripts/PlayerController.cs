using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]

public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions playerControls;

    private InputAction moveAction;
    private Vector2 movementInput;

    [SerializeField] private GameObject player;
    private Rigidbody playerRb;

    void Awake()
    {
        playerControls = new InputSystem_Actions();
        
        playerRb = gameObject.GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        playerControls.Enable();
        moveAction = playerControls.Player.Move;
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
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
        playerRb.MovePosition(playerRb.position +
            new Vector3(moveValue.x, 0, moveValue.y) * Time.fixedDeltaTime);
    }
}
