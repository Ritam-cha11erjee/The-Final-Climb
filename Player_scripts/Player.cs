using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class Player : MonoBehaviour
{
    public Animator playerAnim;
    public Rigidbody playerRigid;
    public float walkSpeed = 5f;
    public float walkBackSpeed = 2f;
    public float originalWalkSpeed;
    public float runSpeedIncrease = 2f;
    public float rotationSpeed = 100f;
    public bool isWalking = false;
    public Transform playerTrans;
    public float mouseLookSpeed = 10f;
    public Transform lookTransform;
    public LayerMask groundLayer;
    public float jumpForce = 3f;
    private bool isMovingLeft = false;
    private bool isMovingRight = false;
    private bool isGrounded = true;
    private bool isJumping = false;
    public int mouseSampleRate = 5;
    private float[] mouseBuffer;
    private int bufferIndex = 0;
    private bool isSprinting = false;
    public bool isBacking = false;
    public bool isIdling = true;

    // Animation flags
    private bool animIsWalking = false;
    private bool animIsRunning = false;
    private bool animIsJumping = false;
    private bool animIsMovingLeft = false;
    private bool animIsMovingRight = false;

    // Camera rotation
    private float currentRotationY = 0f;
    public float rotationSmoothTime = 0.08f;
    private float rotationYVelocity;
    public Transform cameraTransform;
    public float verticalLookSpeed = 5f;
    public float verticalLookLimit = 80f;
    private float[] cameraXRotationBuffer;
    private int cameraXBufferIndex = 0;
    private float[] cameraYRotationBuffer;
    private int cameraYBufferIndex = 0;
    private float smoothedXRotation = 0f;
    private float smoothedYRotation = 0f;
    public int cameraSmoothFrames = 4;
    private float cameraXRotationVelocity;

    // Collider resize
    private CapsuleCollider capsuleCollider;
    private float originalColliderRadius;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    public float jumpColliderHorizontalScale = 0.7f;
    public float jumpColliderVerticalShift = 0.2f;
    public float colliderResizeSpeed = 6f;
    private bool isResizingCollider = false;
    private float targetColliderRadius;
    private float targetColliderHeight;
    private Vector3 targetColliderCenter;

    // Movement smoothing
    private Vector3 movementSmoothVelocity = Vector3.zero;
    public float movementSmoothTime = 0.1f;

    [Header("Ledge Climbing")]
    public float ledgeDetectDistance = 1f;
    public float ledgeDetectHeight = 1.5f;
    public LayerMask ledgeLayer;
    public Transform hangOrigin;
    public float climbUpOffset = 1.2f;
    public float climbDuration = 0.5f;

    private bool isHanging = false;
    private Vector3 hangPoint;
    private Vector3 hangNormal;

    [Header("Ground Check Debug Ray")]
    public float groundedRaycastDistance = 0.15f;
    public Vector3 groundedRaycastOffset; // IMPORTANT: Offset from player's transform.position to the CENTER of the ground check capsule
    public float groundedSkinOffset = 0.05f;

    [Header("Cursor Settings")]
    public bool lockAndHideCursorOnStart = true;


    private void Start()
    {
        if (lockAndHideCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        playerRigid = GetComponent<Rigidbody>();
        playerTrans = transform;
        originalWalkSpeed = walkSpeed;
        mouseBuffer = new float[mouseSampleRate];
        cameraXRotationBuffer = new float[cameraSmoothFrames];
        cameraYRotationBuffer = new float[cameraSmoothFrames];

        if (playerRigid != null)
        {
            playerRigid.interpolation = RigidbodyInterpolation.Interpolate;
            playerRigid.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            Debug.LogError("Capsule Collider not found on the Player!");
            enabled = false;
            return;
        }
        originalColliderRadius = capsuleCollider.radius;
        originalColliderHeight = capsuleCollider.height;
        originalColliderCenter = capsuleCollider.center;
    }

    private void FixedUpdate()
    {
        CheckGrounded();

        Vector3 moveDirection = Vector3.zero;
        Vector3 cameraForward = GetCameraForward();
        Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;

        if (Input.GetKey(KeyCode.W) && !isBacking)
        {
            moveDirection += cameraForward * walkSpeed;
            animIsWalking = true;
            isBacking = false;
            isIdling = false;
        }
        else animIsWalking = false;

        if (Input.GetKey(KeyCode.S))
        {
            moveDirection += -cameraForward * walkBackSpeed;
            playerAnim.SetTrigger("back");
            isBacking = true;
            isIdling = false;
        }
        else if (isBacking)
        {
            playerAnim.ResetTrigger("back");
            playerAnim.SetTrigger("idle");
            isBacking = false;
            isIdling = true;
        }

        if (Input.GetKey(KeyCode.A) && !isBacking)
        {
            moveDirection += -cameraRight * walkSpeed;
            animIsMovingLeft = true;
            isIdling = false;
        }
        else animIsMovingLeft = false;

        if (Input.GetKey(KeyCode.D) && !isBacking)
        {
            moveDirection += cameraRight * walkSpeed;
            animIsMovingRight = true;
            isIdling = false;
        }
        else animIsMovingRight = false;

        if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) &&
            !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D) && !isJumping && isGrounded)
        {
            isIdling = true;
        }

        Vector3 targetMove = moveDirection;
        targetMove.y = playerRigid.linearVelocity.y;
        Vector3 smoothVel = Vector3.SmoothDamp(
            playerRigid.linearVelocity, targetMove,
            ref movementSmoothVelocity,
            isGrounded ? movementSmoothTime : movementSmoothTime * 1.5f);
        playerRigid.linearVelocity = smoothVel;

        float mouseX = Input.GetAxis("Mouse X") * mouseLookSpeed;
        cameraYRotationBuffer[cameraYBufferIndex % cameraSmoothFrames] = mouseX;
        cameraYBufferIndex++;
        float avgX = 0;
        for (int i = 0; i < cameraSmoothFrames; i++) avgX += cameraYRotationBuffer[i];
        avgX /= cameraSmoothFrames;
        currentRotationY += avgX * Time.fixedDeltaTime * 60f;
        float smoothY = Mathf.SmoothDampAngle(
            playerTrans.eulerAngles.y, currentRotationY,
            ref rotationYVelocity, rotationSmoothTime);
        playerTrans.eulerAngles = new Vector3(
            playerTrans.eulerAngles.x, smoothY, playerTrans.eulerAngles.z);
    }

    private void Update()
    {
        Vector3 rayOriginForDebug = transform.position + groundedRaycastOffset + Vector3.up * groundedSkinOffset;
        Debug.DrawRay(rayOriginForDebug, Vector3.down * (groundedRaycastDistance + groundedSkinOffset), isGrounded ? Color.green : Color.red);

        // Debug.Log("Is Grounded: " + isGrounded); 

        if (isHanging)
        {
            if (Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.W))
                StartCoroutine(ClimbLedge());
            return;
        }

        if (Input.GetKey(KeyCode.W) && Input.GetKeyDown(KeyCode.Space) && !isJumping)
        {
            if (TryFindLedge())
            {
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping)
        {
            isJumping = true;
            animIsJumping = true;
            playerAnim.SetTrigger("jump");
            isIdling = false;
            isGrounded = false;

            playerRigid.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            playerRigid.AddForce(GetCameraForward() * 10, ForceMode.Impulse);
            ResizeForJump();
            StartCoroutine(ResetJumpState());
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !isJumping && !isGrounded)
        {
            TryFindLedge();
        }

        UpdateCameraRotation();
        if (isResizingCollider) ResizeCollider();
        HandleInputTriggers();
        UpdateAnimatorParameters();
    }

    [Header("Ground Check Settings")]
    public float groundedOverlapRadius = 0.18f;
    public float groundedOverlapHeightOffset = 0.1f; // Half the height of the capsule's cylinder
    public Vector3 groundedCheckOffset = Vector3.zero; // Offset from the player's transform.position to the CENTER of the ground check capsule

    private void CheckGrounded()
    {
        isGrounded = false;

        // Calculate the center of the overlap capsule in world space
        Vector3 capsuleCenter = transform.position + groundedCheckOffset;

        // Calculate the bottom and top points of the capsule for OverlapCapsule
        Vector3 capsuleBottom = capsuleCenter - Vector3.up * groundedOverlapHeightOffset;
        Vector3 capsuleTop = capsuleCenter + Vector3.up * groundedOverlapHeightOffset;

        // Perform the overlap check, ignoring trigger colliders
        Collider[] colliders = Physics.OverlapCapsule(capsuleBottom, capsuleTop, groundedOverlapRadius, groundLayer, QueryTriggerInteraction.Ignore);

        if (colliders.Length > 0)
        {
            isGrounded = true;
            // Optional: Add debug logging here to see what's being hit
            // if (Time.frameCount % 30 == 0)
            // {
            //     Debug.Log("Grounded by: " + colliders[0].gameObject.name);
            // }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw the ground check overlap capsule for visualization in the editor
        Gizmos.color = Color.yellow;

        Vector3 capsuleCenter = transform.position + groundedCheckOffset;
        Vector3 p1 = capsuleCenter - Vector3.up * groundedOverlapHeightOffset;
        Vector3 p2 = capsuleCenter + Vector3.up * groundedOverlapHeightOffset;

        Gizmos.DrawWireSphere(p1, groundedOverlapRadius);
        Gizmos.DrawWireSphere(p2, groundedOverlapRadius);
        // Connect the spheres to visualize the capsule
        Gizmos.DrawLine(p1 + transform.right * groundedOverlapRadius, p2 + transform.right * groundedOverlapRadius);
        Gizmos.DrawLine(p1 - transform.right * groundedOverlapRadius, p2 - transform.right * groundedOverlapRadius);
        Gizmos.DrawLine(p1 + transform.forward * groundedOverlapRadius, p2 + transform.forward * groundedOverlapRadius);
        Gizmos.DrawLine(p1 - transform.forward * groundedOverlapRadius, p2 - transform.forward * groundedOverlapRadius);
    }


    private bool TryFindLedge()

    {

        Vector3 origin = hangOrigin.position;

        float forwardCheckDistance = ledgeDetectDistance * 0.8f; // Check slightly closer initially



        // 1. Primary Forward-Downward Check

        if (Physics.Raycast(origin, transform.forward + Vector3.down * 0.5f, out RaycastHit initialHit,

              forwardCheckDistance, ledgeLayer))

        {

            // 2. Confirm Ledge Lip with Downward Raycast

            Vector3 downOrigin = initialHit.point + Vector3.up * ledgeDetectHeight;

            if (Physics.Raycast(downOrigin, Vector3.down, out RaycastHit lipHit,

                      ledgeDetectHeight + 0.2f, ledgeLayer))

            {

                hangPoint = lipHit.point;

                hangNormal = lipHit.normal;

                GrabLedge();

                return true;

            }

            else

            {

                // Optional: Try a slightly higher origin for downward check if lip not found immediately

                downOrigin = initialHit.point + Vector3.up * (ledgeDetectHeight + 0.3f);

                if (Physics.Raycast(downOrigin, Vector3.down, out lipHit,

                          ledgeDetectHeight + 0.3f, ledgeLayer))

                {

                    hangPoint = lipHit.point;

                    hangNormal = lipHit.normal;

                    GrabLedge();

                    return true;

                }

            }

        }



        // 3. Fallback: Wider Vertical Scan (less preferred, but can catch some cases)

        float verticalTolerance = 1.5f;

        for (float yOffset = -verticalTolerance; yOffset <= verticalTolerance; yOffset += 1f) // Smaller steps

        {

            Vector3 adjustedOrigin = origin + Vector3.up * yOffset;

            if (Physics.Raycast(adjustedOrigin, transform.forward, out RaycastHit hit,

                      ledgeDetectDistance, ledgeLayer))

            {

                Vector3 downOriginFallback = hit.point + Vector3.up * ledgeDetectHeight;

                if (Physics.Raycast(downOriginFallback, Vector3.down, out RaycastHit lipFallback,

                          ledgeDetectHeight + 0.2f, ledgeLayer))

                {

                    hangPoint = lipFallback.point;

                    hangNormal = lipFallback.normal;

                    GrabLedge();

                    return true;

                }

            }

        }



        return false;

    }



    private void GrabLedge()

    {

        isHanging = true;

        playerRigid.isKinematic = true;

        playerRigid.linearVelocity = Vector3.zero;



        // Calculate target Y for hanging (slightly below the lip)

        float targetY = hangPoint.y - 0.5f; // Adjust 0.5f as needed



        // Calculate starting position based on player's approach direction

        Vector3 approachDirection = -transform.forward;

        Vector3 startHangPositionXZ = new Vector3(hangPoint.x + approachDirection.x * 1.2f, targetY, hangPoint.z + approachDirection.z * 1.2f); // Adjust 1.2f



        transform.position = startHangPositionXZ;



        // *REMOVED ROTATION FOR NOW*

        // Quaternion targetRotation = Quaternion.LookRotation(-hangNormal, Vector3.up);

        // transform.rotation = targetRotation; // Or a smoother Lerp if desired



        playerAnim.Play("Hang");

        StartCoroutine(ClimbLedge()); // Immediately start the climb

    }



    private IEnumerator ClimbLedge()

    {

        playerAnim.Play("climb_anim");

        Vector3 startClimbPosition = transform.position;

        Vector3 ledgeTopPosition = hangPoint + Vector3.up * climbUpOffset + hangNormal * -0.1f;

        Vector3 endClimbPosition = new Vector3(ledgeTopPosition.x, ledgeTopPosition.y, ledgeTopPosition.z);



        float elapsed = 0f;

        while (elapsed < climbDuration)

        {

            float t = elapsed / climbDuration;

            Vector3 currentPosition = Vector3.Lerp(startClimbPosition, endClimbPosition, t);

            transform.position = currentPosition;

            playerRigid.position = currentPosition; // Make the Rigidbody follow

            elapsed += Time.deltaTime;

            yield return null;

        }



        // Ensure both transform and Rigidbody are at the final position

        transform.position = endClimbPosition;

        playerRigid.position = endClimbPosition;



        // Re-enable physics and reset state

        playerRigid.isKinematic = false;

        playerRigid.useGravity = true;



        isHanging = false;

        isGrounded = true;

        playerAnim.Play("Idle_anim");

    }

    private void ResizeForJump()
    {
        targetColliderRadius = originalColliderRadius;
        targetColliderHeight = originalColliderHeight * jumpColliderHorizontalScale;
        targetColliderCenter = originalColliderCenter + Vector3.up * jumpColliderVerticalShift;
        isResizingCollider = true;
    }

    private void ResizeCollider()
    {
        capsuleCollider.radius = Mathf.Lerp(capsuleCollider.radius, targetColliderRadius, Time.deltaTime * colliderResizeSpeed);
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetColliderHeight, Time.deltaTime * colliderResizeSpeed);
        capsuleCollider.center = Vector3.Lerp(capsuleCollider.center, targetColliderCenter, Time.deltaTime * colliderResizeSpeed);
        if (Mathf.Abs(capsuleCollider.radius - targetColliderRadius) < 0.01f &&
            Mathf.Abs(capsuleCollider.height - targetColliderHeight) < 0.01f &&
            (capsuleCollider.center - targetColliderCenter).sqrMagnitude < 0.0001f)
        {
            capsuleCollider.radius = targetColliderRadius;
            capsuleCollider.height = targetColliderHeight;
            capsuleCollider.center = targetColliderCenter;
            isResizingCollider = false;
        }
    }

    private void UpdateAnimatorParameters()
    {
        playerAnim.SetBool("IsWalking", animIsWalking);
        playerAnim.SetBool("IsRunning", animIsRunning);
        playerAnim.SetBool("IsJumping", animIsJumping);
        playerAnim.SetBool("IsMovingLeft", animIsMovingLeft);
        playerAnim.SetBool("IsMovingRight", animIsMovingRight);
        playerAnim.SetBool("IsGrounded", isGrounded);
        playerAnim.SetBool("IsBacking", isBacking);
        playerAnim.SetBool("IsIdling", isIdling && isGrounded && !isJumping);
    }

    private void HandleInputTriggers()
    {
        if (Input.GetKey(KeyCode.W) && !isJumping && !isBacking)
        {
            playerAnim.SetTrigger("walk");
            playerAnim.ResetTrigger("idle");
            animIsWalking = true;
            isIdling = false;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                walkSpeed = originalWalkSpeed + runSpeedIncrease;
                playerAnim.SetTrigger("run");
                playerAnim.ResetTrigger("walk");
                animIsRunning = true;
            }
            else
            {
                walkSpeed = originalWalkSpeed;
                playerAnim.ResetTrigger("run");
                animIsRunning = false;
            }
        }
        else
        {
            playerAnim.ResetTrigger("walk");
            if (animIsRunning)
            {
                playerAnim.ResetTrigger("run");
                animIsRunning = false;
                walkSpeed = originalWalkSpeed;
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) && animIsRunning)
        {
            walkSpeed = originalWalkSpeed;
            playerAnim.ResetTrigger("run");
            if (animIsWalking)
            {
                playerAnim.SetTrigger("walk");
            }
            animIsRunning = false;
        }

        if (Input.GetKeyDown(KeyCode.A) && !isJumping && !isBacking)
        {
            isMovingLeft = true;
            playerAnim.SetTrigger("left");
            isIdling = false;
        }
        if (Input.GetKeyUp(KeyCode.A)) { isMovingLeft = false; }

        if (Input.GetKeyDown(KeyCode.D) && !isJumping && !isBacking)
        {
            isMovingRight = true;
            playerAnim.SetTrigger("right");
            isIdling = false;
        }
        if (Input.GetKeyUp(KeyCode.D)) { isMovingRight = false; }
    }

    private void UpdateCameraRotation()
    {
        float mouseY = Input.GetAxis("Mouse Y") * verticalLookSpeed;
        cameraXRotationBuffer[cameraXBufferIndex % cameraSmoothFrames] = -mouseY;
        cameraXBufferIndex++;
        float avgY = 0f;
        for (int i = 0; i < cameraSmoothFrames; i++) avgY += cameraXRotationBuffer[i];
        avgY /= cameraSmoothFrames;
        smoothedXRotation += avgY * Time.deltaTime * 60f;
        smoothedXRotation = Mathf.Clamp(smoothedXRotation, -verticalLookLimit, verticalLookLimit);
        if (cameraTransform != null)
        {
            float currentXAngle = cameraTransform.localEulerAngles.x;
            float smoothXAngle = Mathf.SmoothDampAngle(
                currentXAngle, smoothedXRotation,
                ref cameraXRotationVelocity, rotationSmoothTime);
            cameraTransform.localRotation = Quaternion.Euler(smoothXAngle, 0f, 0f);
        }
    }

    private Vector3 GetCameraForward()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.forward;
        Vector3 forward = cam.transform.forward;
        forward.y = 0;
        return forward.normalized;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Let CheckGrounded handle isGrounded status
    }

    private void OnCollisionExit(Collision collision)
    {
        // Let CheckGrounded handle isGrounded status
    }
    private IEnumerator ResetJumpState()
    {
        yield return new WaitForSeconds(0.4f);
        isJumping = false;
        animIsJumping = false;

        targetColliderRadius = originalColliderRadius;
        targetColliderHeight = originalColliderHeight;
        targetColliderCenter = originalColliderCenter;
        isResizingCollider = true;
    }
}