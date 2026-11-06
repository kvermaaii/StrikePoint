using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    float playerHeight = 2f;

    [SerializeField] Transform orientation;

    private GrapplingGun grapplingGun;
    private Sliding slide;
    private WallRun wallrun;
    private GunSystem gunsystem;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float airMultiplier = 0.4f;
    public float movementMultiplier = 10f;

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    [Header("Sprinting")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 6f;
    [SerializeField] float acceleration = 10f;

    [Header("Jumping")]
    public float jumpForce = 5f;

    [Header("Keybinds")]
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;


    [Header("Drag")]
    [SerializeField] float groundDrag = 6f;
    [SerializeField] float airDrag = 2f;
    [SerializeField] float gravityMultiplier;

    [Header("Grapple Momentum")]
    [Tooltip("Rigidbody drag while the rope is attached and airborne. The normal airDrag (2) removes ~13% of your speed every 0.07s, which kills a swing.")]
    public float grappleAirDrag = 0.6f;
    [Tooltip("After the rope is released (or jumped off) keep the low drag for this long, so the release arc keeps its swing speed.")]
    public float grappleMomentumTime = 0.5f;
    [Tooltip("For this long after the rope was last attached, touching the ground uses landingDrag instead of groundDrag, so a landing-jump keeps horizontal speed.")]
    public float landingCarryTime = 0.2f;
    public float landingDrag = 2f;
    [Tooltip("Pressing jump while the rope is attached releases it and jumps, keeping the swing's horizontal speed.")]
    public bool jumpFromGrapple = true;

    float lastGrappleTime = -999f;

    float horizontalMovement;
    float verticalMovement;

    [Header("Ground Detection")]
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundDistance = 0.2f;
    public bool isGrounded;

    Vector3 moveDirection;
    Vector3 slopeMoveDirection;


    Rigidbody rb;

    RaycastHit slopeHit;

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight / 2 + 0.5f, groundMask))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle > 0.05f && angle <= 55f;
        }
        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private IEnumerable SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            time += Time.deltaTime;
            yield return null;
        }
        moveSpeed = desiredMoveSpeed;
    }

    private void Start()
    {
        slide = GetComponent<Sliding>();
        wallrun = GetComponent<WallRun>();
        rb = GetComponent<Rigidbody>();
        grapplingGun = GetComponentInChildren(typeof(GrapplingGun)) as GrapplingGun;
        rb.freezeRotation = true;

        // Ensure frictionless physics material to prevent snagging on walls, bevels, and pillars
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            PhysicsMaterial smoothMat = new PhysicsMaterial("PlayerZeroFriction");
            smoothMat.dynamicFriction = 0f;
            smoothMat.staticFriction = 0f;
            smoothMat.frictionCombine = PhysicsMaterialCombine.Minimum;
            smoothMat.bounceCombine = PhysicsMaterialCombine.Minimum;
            col.material = smoothMat;
        }

        // Ignore collisions between Player (Layer 10) and Target (Layer 12) so targets never physically obstruct movement
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            Physics.IgnoreLayerCollision(playerLayer, 12, true);
        }
        Physics.queriesHitTriggers = true;
    }


    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        MyInput();
        ControlDrag();
        ControlSpeed();
        
        if (Input.GetKeyDown(jumpKey))
        {
            TryJump();
        }

        slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal);
    }

    void MyInput()
    {
        horizontalMovement = Input.GetAxis("Horizontal");
        verticalMovement = Input.GetAxis("Vertical");

        moveDirection = orientation.forward * verticalMovement + orientation.right * horizontalMovement;
    }

    /// <summary>Jump from the ground, or - while swinging - release the rope and jump off it.</summary>
    public void TryJump()
    {
        if (isGrounded)
        {
            Jump();
        }
        else if (jumpFromGrapple && grapplingGun != null && grapplingGun.IsGrappling())
        {
            SwingJump();
        }
    }

    // Jump off the rope mid-swing. Unlike Jump() this keeps any upward speed the swing has, and the horizontal speed is untouched.
    void SwingJump()
    {
        grapplingGun.StopGrapple();
        lastGrappleTime = Time.time;                       // keep the low-drag window running after the release
        Vector3 v = rb.linearVelocity;
        v.y = Mathf.Max(v.y, 0f);                          // drop downward speed (like a ground jump) but never cut upward speed
        rb.linearVelocity = v;
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        if (OnSlope() && slide.sliding)
        {
            rb.AddForce(orientation.forward * jumpForce, ForceMode.Impulse);
        }
    }

    void ControlSpeed()
    {
        if (Input.GetKey(sprintKey) && isGrounded)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, acceleration * Time.deltaTime);
        }
    }

    void ControlDrag()
    {
        bool grappling = grapplingGun != null && grapplingGun.IsGrappling();
        if (grappling) lastGrappleTime = Time.time;
        float sinceGrapple = Time.time - lastGrappleTime;

        if (isGrounded)
            rb.linearDamping = sinceGrapple < landingCarryTime ? landingDrag : groundDrag;
        else
            rb.linearDamping = (grappling || sinceGrapple < grappleMomentumTime) ? grappleAirDrag : airDrag;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }
    void MovePlayer()
    {
        if (isGrounded && !OnSlope())
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier, ForceMode.Acceleration);
        }
        else if (isGrounded && OnSlope())
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * movementMultiplier, ForceMode.Acceleration);
        }
        else if (wallrun.wallrunning)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier * airMultiplier, ForceMode.Acceleration);
        }
        else if (!isGrounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier * airMultiplier, ForceMode.Acceleration);
            rb.AddForce(-orientation.up * gravityMultiplier * movementMultiplier * airMultiplier, ForceMode.Acceleration);
        }
    }

    // Checkpoint after the 90-degree corner wall run in section A of the Mid level (first flat floor of corridor B).
    // The position comes from the "Checkpoint_Mid_AfterWallRun" marker so it can be moved in the editor; the fallback matches its default.
    private Vector3 MidWallRunCheckpoint()
    {
        GameObject marker = GameObject.Find("Checkpoint_Mid_AfterWallRun");
        return marker != null ? marker.transform.position : new Vector3(-124.2f, 13.3f, 116.0f);
    }

    private void OnTriggerEnter(Collider collider)
    {
        //HARD LEVEL TELEPORTERS---------------------------------------------------------
        if (collider.gameObject.name == "Plane0")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(1.0f, 2.47f, 0.25f);
        }
        if (collider.gameObject.name == "Plane1")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(2.07f, -6.73f, 139.4f);
        }
        if (collider.gameObject.name == "Plane2")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(195.1f, -19.24f, 139.4f);
        }
        if (collider.gameObject.name == "Plane3")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(195.1f, -19.24f, 139.4f);
        }
        if (collider.gameObject.name == "Plane4")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(195.1f, -19.24f, 139.4f);
        }
        if (collider.gameObject.name == "Plane5")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(219.2f, 42.01f, 2.85f);
        }
        if (collider.gameObject.name == "Plane6")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(219.2f, 42.01f, 2.85f);
        }
        if (collider.gameObject.name == "Plane7")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(76.53f, 8.29f, -87.96f);
        }
        //-------------------------------------------------------------------------------

        //MEDIUM LEVEL TELEPORTERS-------------------------------------------------------
        if (collider.gameObject.name == "PlaneMid0")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(1.5f, 1.5f, 1.5f);
        }
        if (collider.gameObject.name == "PlaneMid1")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(-17.43f, 4.4f, 96.24f);
        }
        if (collider.gameObject.name == "PlaneMid2")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(-17.43f, 4.4f, 96.24f);
        }
        // PlaneMid2c is the off-route strip north of the wall-run; it is before the new checkpoint, so it uses checkpoint 1 like PlaneMid2.
        if (collider.gameObject.name == "PlaneMid2c")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(-17.43f, 4.4f, 96.24f);
        }
        // Checkpoint after the 90-degree corner wall run in section A, on the first flat floor of corridor B.
        // PlaneMid2b covers corridor B's south end (z >= 111.5); PlaneMid3 covers the rest of B, the waterfall room and the pad
        // gallery, all of which are reached after this checkpoint. Falls before it (PlaneMid2, over the corner wall run) still use checkpoint 1.
        if (collider.gameObject.name == "PlaneMid2b")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = MidWallRunCheckpoint();
        }
        if (collider.gameObject.name == "PlaneMid3")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = MidWallRunCheckpoint();
        }
        if (collider.gameObject.name == "PlaneMid4")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(-15.23f, 11.23f, 199.776f);
        }
        if (collider.gameObject.name == "PlaneMid5")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(103.62f, 11.23f, 171.04f);
        }
        if (collider.gameObject.name == "PlaneMid6")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(215.79f, 6.21f, 200.99f);
        }
        //-------------------------------------------------------------------------------

        //EASY LEVEL TELEPORTERS-------------------------------------------------------
        if (collider.gameObject.name == "Plane0_easy")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(7.8f, 15.47f, -44.2f);
        }
        if (collider.gameObject.name == "Plane1_easy")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(7.8f, -12.37f, 181.59f);
        }
        if (collider.gameObject.name == "Plane2_easy")
        {
            grapplingGun.StopGrapple();
            rb.transform.position = new Vector3(7.8f, -22.2f, 332.6f);
        }


    }
}
