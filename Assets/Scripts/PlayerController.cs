//using System.Numerics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]

public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions playerControls;

    private InputAction moveAction;
    private Vector2 movementInput;

    [SerializeField] private GameObject player;
    private Rigidbody playerRB;

    [SerializeField] private float jumpForce = 50f;

    // Movement acceleration variables
    [SerializeField] private float baseSpeed = 2f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float accelerationTime = 3f; // Time to reach max speed
    private float currentSpeed = 0f;

    // Spring connection variables
    //[SerializeField] private GameObject grapplePoint;
    //[SerializeField] private bool createSpringOnStart = true;
    [SerializeField] private float springForce = 200f;
    [SerializeField] private float damper = 7f;
    [SerializeField] private float massScale = 25f;
    //[SerializeField] private float minDistance = 0f;

    [SerializeField] private float maxGrappleRadius = 10f;
    
    private SpringJoint springJoint;

    private LineRenderer lineRenderer;

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
        playerControls.Player.Quit.performed += QuitAction;
        playerControls.Player.Grapple.started += GrappleAction;
        playerControls.Player.Grapple.canceled += GrappleAction;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        playerControls.Player.Grapple.started -= GrappleAction;
        playerControls.Player.Grapple.canceled -= GrappleAction;
        playerControls.Player.Quit.performed -= QuitAction;
        playerControls.Player.Jump.performed -= JumpAction;
        playerControls.Disable();
    }

    void Start()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        springJoint = null;
        //moveAction = InputSystem.actions.FindAction("move");
        //Vector2 moveValue = moveAction.ReadValue<Vector2>();
        //Debug.Log("Move Value: " + moveValue);

    }
    void Update()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        movementInput = moveValue;
        bool isButtonInput = IsButtonOrKeyInput(moveAction.activeControl);

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

        /*if (Input.GetMouseButtonDown(0))
        {
            GrappleStart();
            DrawGrappleLine();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            GrappleEnd();
        }*/
    }
    void FixedUpdate()
    {
        //playerRB.MovePosition(playerRB.position +
        //    new Vector3(movementInput.x, 0, movementInput.y) * currentSpeed * Time.fixedDeltaTime);
        // Use velocity instead of MovePosition for proper collision detection
        //Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y) * currentSpeed;
        //playerRB.linearVelocity = new Vector3(movement.x, playerRB.linearVelocity.y, movement.z);

        // Only apply movement when grounded or when there's input
        bool isGrounded = Physics.Raycast(player.transform.position, Vector3.down, 1.1f);
        
        if (isGrounded || movementInput.magnitude > 0.1f)
        {
            Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y) * currentSpeed;
            
            if (isGrounded)
            {
                // Full control when grounded
                playerRB.linearVelocity = new Vector3(movement.x, playerRB.linearVelocity.y, movement.z);
            }
            else
            {
                // Reduced air control - add to existing velocity instead of replacing
                Vector3 airMovement = movement * 0.3f; // Reduce air control strength
                playerRB.linearVelocity = new Vector3(
                    playerRB.linearVelocity.x + airMovement.x * Time.fixedDeltaTime,
                    playerRB.linearVelocity.y,
                    playerRB.linearVelocity.z + airMovement.z * Time.fixedDeltaTime
                );
            }
        }
        if (springJoint != null)
        {
            lineRenderer.SetPosition(0, gameObject.transform.position);
        }
    }
    public void GrappleStart()
    {
        if (springJoint != null)
        {
            return;
        }
        Collider[] hitColliders = Physics.OverlapSphere(gameObject.transform.position,
            maxGrappleRadius, LayerMask.GetMask("GrapplePoint"));

        float closestDistance = Mathf.Infinity;
        Collider closestCollider = null;
        for (int i = 0; i < hitColliders.Length; i++)
        {
            //Debug.Log("Hit collider: " + hitColliders[i].gameObject.name);
            //Debug.Log("Distance to grapple point: " +
            //    Vector3.Distance(gameObject.transform.position, hitColliders[i].transform.position));
            float distance = Vector3.Distance(gameObject.transform.position, hitColliders[i].transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCollider = hitColliders[i];
            }

        }
        if (closestCollider != null)
        {
            //Debug.Log("Grapple point within range. Creating spring connection.");
            Vector3 grapplePointPos = closestCollider.transform.position;
            springJoint = gameObject.AddComponent<SpringJoint>();
            springJoint.autoConfigureConnectedAnchor = false;
            springJoint.connectedAnchor = grapplePointPos;

            float distanceFromPoint = Vector3.Distance(gameObject.transform.position, grapplePointPos);

            // distance grapple will try to keep from grapple point
            springJoint.maxDistance = distanceFromPoint * 0.8f;
            springJoint.minDistance = distanceFromPoint * 0.10f; //0.25f;
            springJoint.spring = springForce;
            springJoint.damper = damper;
            springJoint.massScale = massScale;
            //
            lineRenderer.positionCount = 2;

        }
    }

    public void GrappleEnd()
    {
        if (springJoint == null)
        {
            return;
        }
        lineRenderer.positionCount = 0;
        Destroy(springJoint);
        springJoint = null;
    }
    void DrawGrappleLine()
    {
        if (springJoint == null)
        {
            return;
        }
        lineRenderer.SetPosition(0, gameObject.transform.position);
        lineRenderer.SetPosition(1, springJoint.connectedAnchor);
    }

    // Input action handlers
    private void GrappleAction(InputAction.CallbackContext context)
    {
        //Debug.Log("Grapple action triggered: " + context.phase);
        if (context.started)
        {
            GrappleStart();
            DrawGrappleLine();
        }
        else if (context.canceled)
        {
            GrappleEnd();
        }
    }
    private void JumpAction(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }
        if (springJoint == null)
        {
            bool isGrounded = Physics.Raycast(player.transform.position, Vector3.down, 1.1f);
            if (!isGrounded)
            {
                return;
            }
        }

            //playerRB.AddForce(Vector3.up * 40, ForceMode.Impulse);
            playerRB.linearVelocity = new Vector3(playerRB.linearVelocity.x, 0, playerRB.linearVelocity.z);
            playerRB.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            // isGrounded = false;

    }
    // Check if the active control is a button or key (not an analog stick)
    private bool IsButtonOrKeyInput(InputControl control)
    {
        if (control == null) return false;

        // Check if it's a button or key control
        return control is ButtonControl ||
               control is KeyControl ||
               (control.device is Keyboard);
    }
    private void QuitAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            QuickQuit();
        }
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
