using System;
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
    private float currentSpeed = 1f;

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

    //private float currentHoldTime = 0f;
    private float startHoldTime = Mathf.Infinity;
    private Vector2 lastMoveValue = Vector2.zero;

    [SerializeField] private int applesCollected = 0;
    [SerializeField] private float velocityMult = 10f;
    [SerializeField] private float maxMagnitude = 3f;

    void Awake()
    {
        playerControls = new InputSystem_Actions();

        playerRB = gameObject.GetComponent<Rigidbody>();
        springJoint = null;
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
        //moveAction = InputSystem.actions.FindAction("move");
        //Vector2 moveValue = moveAction.ReadValue<Vector2>();
        //Debug.Log("Move Value: " + moveValue);

    }
    void Update()
    {
        // fell out of world?
        if (transform.position.y < -8f)
        {
            QuickQuit();
        }

        Vector2 moveValue = moveAction.ReadValue<Vector2>();        
        bool isButtonInput = IsButtonOrKeyInput(moveAction.activeControl);

        if (isButtonInput)
        {
            if (startHoldTime == Mathf.Infinity) startHoldTime = Time.time;
            movementInput = DigitalToAnalogInput(moveValue, lastMoveValue, ref startHoldTime, baseSpeed, maxSpeed, accelerationTime);
            //Debug.Log("Movement Input: " + movementInput + " (Raw: " + moveValue + ")");
        }
        else
        {
            movementInput = moveValue;
        }
        lastMoveValue = moveValue;

        // Pressing up while grappled = simulate 'jump'
        // TODO: (should probably draw it in towards grapple point instead when Y position above grapple point)
        if (movementInput.y > 0.1f && springJoint != null)
        {
            //Debug.Log("Move Value: " + moveValue);
            playerRB.linearVelocity = new Vector3(playerRB.linearVelocity.x, 0, playerRB.linearVelocity.z);
            playerRB.AddForce(Vector3.up * 2, ForceMode.Impulse);
        }

        //if (Input.GetMouseButtonDown(0))
        //{ GrappleStart(); DrawGrappleLine(); }
        //else if (Input.GetMouseButtonUp(0))
        //{ GrappleEnd(); }
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
    // This can be handled better (especially the startTime ref)
    Vector2 DigitalToAnalogInput(Vector2 digitalInput, Vector2 lastMoveValue, ref float startTime, float startSpeed, float maxSpeed, float accelTime)
    {
        //Debug.Log("Digital Input: " + digitalInput + " Last Move Value: " + lastMoveValue + " Start Time: " + startTime);
        // No movement input = 0 speed
        if (digitalInput.magnitude < 0.1f)
        {
            //Debug.Log("No input - returning zero");
            startTime = Mathf.Infinity;
            return Vector2.zero;
        }
        // x value moved in opposite direction?
        if (lastMoveValue.x < 0.1f && digitalInput.x > 0.1f ||
            lastMoveValue.x > 0.1f && digitalInput.x < 0.1f)
        {
            //Debug.Log("X direction changed");
            // y value direction changed?
            if (lastMoveValue.y < 0.1f && digitalInput.y > 0.1f ||
                lastMoveValue.y > 0.1f && digitalInput.y < 0.1f)
            {
                // Reset speed * start time
                startTime = Time.time;
                return digitalInput.normalized * startSpeed;
            }
        }
        // y value moved in opposite direction?
        else if (lastMoveValue.y < 0.1f && digitalInput.y > 0.1f ||
            lastMoveValue.y > 0.1f && digitalInput.y < 0.1f)
        {
            //Debug.Log("Y direction changed");
            // (x value hasn't changed direction if we're here)
            // however if no x input is being applied, reset speed
            if (Mathf.Abs(digitalInput.x) < 0.1f)
            {
                // Reset speed * start time
                startTime = Time.time;
                return digitalInput.normalized * startSpeed;
            }
            // else fall through to continue accelerating
        }
        //Debug.Log("Continuing acceleration");
        float holdTime = Time.time - startTime;
        float speed = Mathf.Lerp(startSpeed, maxSpeed, Mathf.Clamp(holdTime / accelTime, 0f, 1f));
        return digitalInput.normalized * speed;
    }
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Apple"))
        {
            applesCollected++;
            Destroy(collision.gameObject);
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
        if (springJoint != null)
        {
            Vector3 distanceToGrapple = gameObject.transform.position - springJoint.connectedAnchor;
            if (distanceToGrapple.magnitude > maxMagnitude)
            {
                distanceToGrapple.Normalize();
                distanceToGrapple *= maxMagnitude;
            }
            //playerRB.collisionDetectionMode = CollisionDetectionMode.Continuous;
            playerRB.linearVelocity = -distanceToGrapple * velocityMult;
            GrappleEnd();
        }
        else
        {
            // Check if grounded before allowing jump
            bool isGrounded = Physics.Raycast(player.transform.position, Vector3.down, 1.1f);
            if (!isGrounded)
            {
                return; // Prevent jump if not grounded
            }
            //playerRB.AddForce(Vector3.up * 40, ForceMode.Impulse);
            playerRB.linearVelocity = new Vector3(playerRB.linearVelocity.x, 0, playerRB.linearVelocity.z);
            playerRB.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            // isGrounded = false;
        }

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
