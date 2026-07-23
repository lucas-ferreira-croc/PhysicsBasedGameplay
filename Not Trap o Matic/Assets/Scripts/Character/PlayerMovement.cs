using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 15.0f;
    public float gravity = 20.0f;
    public float jump = 12.0f;
    private Vector3 moveVelocity;

    public float cameraAcceleration = 40.0f;
    public float mouseSense = 0.1f;
    private float xCameraRotation = 0.0f;


    private Vector3 direction;
    private Vector3 gravityVec;

    [SerializeField]
    private Transform head;
    [SerializeField]
    private Transform cameraHolder;
    private Transform camHolderParent;
    private Collider playerCollider;
    GrappleHook grappleHook;

    // [SerializeField] GrappleHook grappleHook;

    enum MOVEMENT_STATE
    {
        GROUND,
        AIR,
        SLIDING,
        WALLRUNNING
    };

    private MOVEMENT_STATE currentState = MOVEMENT_STATE.GROUND;
    private MOVEMENT_STATE previousState = MOVEMENT_STATE.GROUND;

    [Header("Ground State")]
    const float floorSnapLength = 0.4f;
    const float floorAccel = 7.0f;
    const float floorDrag = 8.0f;
    Vector3 floorNormal;

    [Header("Air State")]
    const float airSnapLength = 0.1f;
    const float airAccel = 0.5f;
    const float airSpeed = 16.0f;
    const float airDrag = 0.1f;

    [Header("Air Strafing")]
    private AnimationCurve airStrafeCurve = new AnimationCurve(
        new Keyframe(0f, 1.49012e-08f, 0f, 0f),
        new Keyframe(0.253521f, 1f, 11.7033f, -5.76162f),
        new Keyframe(0.5f, 1.49012e-08f, -0.0482608f, -0.0482608f),
        new Keyframe(1f, 1.49012e-08f, 0f, 0f)
    );


    const float minStrafeAngle = 0.0f;
    const float maxStrafeAngle = 180.0f;
    const float airStrafeModifier = 1.0f;

    [Header("Jumping")]
    bool canJump = true;
    bool hasJumped = false;
    float coyoteTime = 0.2f;
    bool jumpQueued = false;
    bool inputJumpPressed = false;
    
    [Header("Crouching")]
    float fullHeight;
    float crouchHeight;
    // ceiling collision, maybe
    // @onready var ceilingCheck: ShapeCast3D = $CeilingCheck
    const float heightLerpSpeed = 10.0f;
    float headOffset;
    bool isCrouching = false;
    const float crouchSpeed = 8.0f;
    const float crouchAccel = 4.0f;

    [Header("Sliding")]
    private AnimationCurve slideDragCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),      
        new Keyframe(1f, 1f, 2.81868f, 0f)
    );
    
    private AnimationCurve slopeAngleDragCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, -4.62323f),
        new Keyframe(0.363158f, 0.197802f, -0.635451f, -0.635451f),
        new Keyframe(1f, 0f, 0f, 0f)
    );
    
    const float slideAccel = 0.8f;
    public float slideCurvePoint = 0.0f;
    const float slideDragTime = 0.6f;
    const float startSlideThreshold = 13.0f;
    const float endSlideSpeed = 11.0f;
    const float slideBoostForce = 4.0f;
    const float slideBoostTime = 2.0f;
    bool canSlideBoost = true;
    const float maxSlideSLopeSpeed = 25.0f;
    const float slideSlopeForce = 4.0f;
    
    [Header("Wall Running")]
    [SerializeField]
    public AnimationCurve wallrunCurve = new AnimationCurve(
        new Keyframe(0f, 1.49012e-08f, 0f, 6.06444f),
        new Keyframe(0.248826f, 0.648352f, 0f, -3.28957f),
        new Keyframe(1f, -0.450549f, 0f, 0f)
    );
    const float wallrunHeight = 4.0f;
    Vector3 wallrunStartVel;
    float wallrunPoint = 0.0f;
    const float wallrunTime = 2.0f;
    const float wallrunResetTime = 1.0f;
    bool hasLeftWallrun = false;
    bool hasRightWallRun = false;
    bool leftWallRun = true;
    Vector3 prevWallNormal;
    Vector3 prevWallRunPoint = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
    private bool isOnWallOnly = false;
    private Vector3 wallNormal = Vector3.zero;
    private Vector3 lastWallCollisionPoint = Vector3.zero;

    [SerializeField] private float shakeMaxSpeed = 20.0f;
    [SerializeField] private float fov = 95.0f;
    [SerializeField] private float speedFovIncrease = 5.0f;
    [SerializeField] private float fovLerpSpeed = 5.0f;

    private CharacterController controller;

    // camera shake? maybe?
    //private ShakerComponent3D cameraShaker;
    PlayerCamera playerCamera;
    //private ShakerComponent3D headBobShaker;


    // events
    public static event Action JustJumped;
    public static event Action JustLanded;
    public static event Action StartSlide;
    public static event Action EndSlide;
    public static event Action<bool> StartWallRun;
    public static event Action<bool> EndWallRun;

    void Awake()
    {
        Transform capsuleObject = transform.Find("Capsule");
        if(capsuleObject != null) 
        {
            playerCollider = capsuleObject.GetComponent<Collider>();
            if(playerCollider is CapsuleCollider capsule)
            {
                fullHeight = capsule.height;
                crouchHeight = fullHeight / 2.0f;
            }
        }

        headOffset = head.position.y;

        if(cameraHolder != null)
        {
            camHolderParent = cameraHolder.parent;
        }

        controller = GetComponent<CharacterController>();
        playerCamera = GetComponent<PlayerCamera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        ProcessMouseInput();
        if (Input.GetButtonDown("Jump"))
        {
            inputJumpPressed = true;
        }

        float currentFPS = 1.0f / Time.unscaledDeltaTime;
        float physicsFPS = 1.0f / Time.fixedDeltaTime;

        if(currentFPS > physicsFPS)
        {
            cameraHolder.parent = null;

            cameraHolder.position = Vector3.Lerp(cameraHolder.position, head.position, cameraAcceleration * Time.deltaTime);

            Vector3 targetRotation = cameraHolder.eulerAngles;
            targetRotation.y = transform.eulerAngles.y;
            targetRotation.x = head.eulerAngles.x;
            cameraHolder.eulerAngles = targetRotation;
        }
        else
        {
            if(cameraHolder.parent != camHolderParent)
            {
                cameraHolder.parent = camHolderParent;
            }

            cameraHolder.position = head.position;
            cameraHolder.rotation = head.rotation;
        }

        /// camera shader and head bob shake session if implemented
        /// 

        float targetFov = Mathf.Lerp(fov, fov + speedFovIncrease, Mathf.Min(controller.velocity.magnitude / shakeMaxSpeed, 1.0f));
        playerCamera.fov = Mathf.Lerp(playerCamera.fov, targetFov, fovLerpSpeed * Time.deltaTime);
    }

    private void FixedUpdate()
    {

        if ((inputJumpPressed || jumpQueued) && canJump)
        {
            canJump = false;
            hasJumped = true;
            jumpQueued = false;
            JustJumped?.Invoke();

            if (currentState != MOVEMENT_STATE.AIR)
            {
                ChangeState(MOVEMENT_STATE.AIR);
            }
        }

        inputJumpPressed = false;

        switch (currentState)
        {
            case (MOVEMENT_STATE.GROUND):
                Ground(Time.fixedDeltaTime);
                break;
            case (MOVEMENT_STATE.AIR):
                Air(Time.fixedDeltaTime);
                break;
            case (MOVEMENT_STATE.SLIDING):
                Slide(Time.fixedDeltaTime);
                break;
            case (MOVEMENT_STATE.WALLRUNNING):
                Air(Time.fixedDeltaTime);
                break;
        }

    }

    void Ground(float delta)
    {
        isCrouching = HandleCrouch(delta);

        float floor_snap_length = floorSnapLength;
        gravityVec = Vector3.zero;

        float velocity = moveVelocity.magnitude;
        if (!isCrouching || (isCrouching && velocity > startSlideThreshold))
        {
            Move(delta, floorAccel, floorDrag);
            if (isCrouching)
            {
                ToSlide();
            }
        }
        else
        {
            Move(delta, crouchAccel, floorDrag);
        }
        GroundToAir();
    }

    void Air(float delta)
    {
        isCrouching = HandleCrouch(delta, false, true);
        float floor_snap_length = airSnapLength;

        if (hasJumped)
        {
            gravityVec = (floorNormal + Vector3.up).normalized * jump;
            hasJumped = false;
        }
        else
        {
            gravityVec = Vector3.down * gravity * delta;
        }
        Move(delta, airAccel, airDrag);

        if(controller.isGrounded /* &&  !grappleHook.IsLaunched()*/)
        {
            if(isCrouching && moveVelocity.magnitude > startSlideThreshold)
            {
                ToSlide();
            }
            else
            {
                ChangeState(MOVEMENT_STATE.GROUND);
                hasLeftWallrun = false;
                hasRightWallRun = false;
                JustLanded?.Invoke();
            }
            canJump = true;
        }


        if (inputJumpPressed)
        {
            QueueJump();
        }

        if(isOnWallOnly /* && grappleHook.IsLaunched()*/)
        {
            if(wallrunPoint >= 1.0)
            {
                return;
            }

            bool leftWall = IsWallRunningLeft(lastWallCollisionPoint);
            if(!(leftWall && hasLeftWallrun) || (!leftWall && hasRightWallRun))
            {
                wallrunPoint = 0.0f;
                if (leftWall)
                {
                    hasLeftWallrun = true;
                    hasRightWallRun = false;
                }
                else
                {
                    hasLeftWallrun = false;
                    hasRightWallRun = true;
                }
            }
            else if(transform.position.y > prevWallRunPoint.y)
            {
                return;
            }

            ChangeState(MOVEMENT_STATE.WALLRUNNING);
            wallrunStartVel = moveVelocity;
        }
    }

    void Slide(float delta)
    {
        isCrouching = HandleCrouch(delta, true);
        if(slideCurvePoint < 1.0f)
        {
            slideCurvePoint += delta / slideDragTime;
        }
        else
        {
            slideCurvePoint = 1.0f;
        }

        gravityVec = Vector3.zero;

        bool isSlideDownward = Vector3.Dot(moveVelocity.normalized, floorNormal) > 0;
        float floorAngle = Vector3.Angle(floorNormal, Vector3.up) * Mathf.Deg2Rad;
        float floorMaxAngle = controller.slopeLimit * Mathf.Deg2Rad;
        float angleCurveSamplePoint = isSlideDownward ? floorAngle / floorMaxAngle : 0.0f;


        Vector3 horizontalVelocity = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
        if(isSlideDownward && horizontalVelocity.magnitude < maxSlideSLopeSpeed)
        {
            ApplyForce(moveVelocity.normalized * delta * slideSlopeForce);
        }

        float clampedSlidePoint = Mathf.Clamp01(slideCurvePoint);
        float clampedAnglePoint = Mathf.Clamp01(angleCurveSamplePoint);

        float slideDrag = slideDragCurve.Evaluate(clampedSlidePoint) * slopeAngleDragCurve.Evaluate(angleCurveSamplePoint);

        Move(delta, slideAccel, slideDrag);

        float finalFrameSpeed = new Vector3(moveVelocity.x, 0f, moveVelocity.z).magnitude;

        if(finalFrameSpeed < endSlideSpeed)
        {
            ChangeState(MOVEMENT_STATE.GROUND);
            slideCurvePoint = 0.0f;
        }

        if (GroundToAir())
        {
            slideCurvePoint = 0.0f;
        }
    }

    void Wallrun(float delta)
    {
        Vector3 leftWallNormal = Quaternion.AngleAxis(90f, Vector3.up) * wallNormal;
        Vector3 rightWallNormal = Quaternion.AngleAxis(-90f, Vector3.up) * wallNormal;

        Vector3 newDirection = (Vector3.Angle(leftWallNormal, wallrunStartVel) < Vector3.Angle(rightWallNormal, wallrunStartVel))
            ? leftWallNormal
            : rightWallNormal;

        //moveVelocity = newDirection.normalized * Mathf.Clamp(wallrunStartVel.magnitude, speed / 2, speed * 2.0f);
        //moveVelocity -= wallNormal * 2.0f;


        Vector3 horizontalVelocity = newDirection.normalized * Mathf.Clamp(wallrunStartVel.magnitude, speed / 2, speed * 1.2f);
        horizontalVelocity -= wallNormal * 1.5f;
        
        float verticalVelocity = wallrunCurve.Evaluate(Mathf.Clamp01(wallrunPoint)) * wallrunHeight;
        moveVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        
        //moveVelocity += Vector3.up * (wallrunCurve.Evaluate(wallrunPoint) * wallrunHeight);
        controller.Move(moveVelocity * delta);

        if (wallrunPoint < 1.0f && isOnWallOnly)
        {
            wallrunPoint += delta / wallrunTime;
        }
        else
        {
            ChangeState(MOVEMENT_STATE.AIR);
            ResetWallRun();
            return;
        }

        if (inputJumpPressed || hasJumped || jumpQueued)
        {
            jumpQueued = false;
            hasJumped = false;
            canJump = false;

            prevWallRunPoint = transform.position;
            ChangeState(MOVEMENT_STATE.AIR);

            floorNormal = Vector3.up;

            //ApplyForce((Vector3.up + wallNormal / 2).normalized * 12.0f);
            ApplyForce((Vector3.up + wallNormal * 1.2f).normalized * jump);
            ResetWallRun();
        }

        prevWallNormal = wallNormal;
    }

    void ResetWallRun()
    {
        if (hasLeftWallrun)
        {
            StartCoroutine(ExecuteAfterTime(wallrunResetTime, () => hasLeftWallrun = false));
        }
        if (hasRightWallRun)
        {
            StartCoroutine(ExecuteAfterTime(wallrunResetTime, () => hasRightWallRun = false));
        }

        if(currentState != MOVEMENT_STATE.WALLRUNNING)
        {
            wallrunPoint = 0.0f;
        }
        prevWallNormal = Vector3.up;
    }

    bool HandleCrouch(float delta, bool forceCrouch = false, bool forceUncrouch = false)
    {
        float height = 0.0f;
        bool crouching = Input.GetKey(KeyCode.LeftControl) || forceCrouch; // possible place to implement ceiling collision
        if (playerCollider is CapsuleCollider capsule) 
        {
            height = capsule.height;
            
            crouching = forceUncrouch ? false : crouching;

            if (height != fullHeight || height != crouchHeight)
            {
                capsule.height = Mathf.Lerp(capsule.height, crouching ? crouchHeight : fullHeight, delta * heightLerpSpeed);
                head.position = new Vector3(head.position.x, Mathf.Lerp(head.position.y, !crouching ? headOffset : headOffset / 2.0f, delta * heightLerpSpeed), head.position.z);
            }

        }
        return crouching;
    }

    void QueueJump()
    {
        jumpQueued = true;
        StartCoroutine(ExecuteAfterTime(coyoteTime, () =>  jumpQueued = false ));
    }

    bool GroundToAir()
    {
        if (!controller.isGrounded)
        {
            ChangeState(MOVEMENT_STATE.AIR);
            if (canJump)
            {
                StartCoroutine(ExecuteAfterTime(coyoteTime, () =>
                {
                    if (!controller.isGrounded)
                    {
                        canJump = false;
                    }
                }));
            }
            return true;
        }
        return false;
    }


    bool ToSlide()
    {
        if (canSlideBoost)
        {
            ApplyForce(moveVelocity.normalized * slideBoostForce);
            canSlideBoost = false;
            StartCoroutine(ExecuteAfterTime(slideBoostTime, () => canSlideBoost = true));
        }

        ChangeState(MOVEMENT_STATE.SLIDING);
        return true;
    }

    bool IsWallRunningLeft(Vector3 collisionPoint)
    {
        Vector3 localCollision = head.InverseTransformPoint(collisionPoint);
        leftWallRun = !(localCollision.x >= 0);
        return localCollision.x >= 0;
    }

    void Move(float delta, float accel, float drag, float speed = 15.0f)
    {
        direction = Vector3.zero;
        float forwardInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");
        direction = transform.right * horizontalInput + transform.forward * forwardInput;
        direction = direction.normalized;

        Vector3 wishVelocity = direction * speed;
        Vector3 velocity = moveVelocity;

        // air strafing
        if (currentState == MOVEMENT_STATE.AIR)
        {
            float angleDiff = GetHorizontalAngle(velocity, wishVelocity);
            float samplePoint = (angleDiff - minStrafeAngle) / maxStrafeAngle;
            wishVelocity *= 1.0f + (airStrafeCurve.Evaluate(samplePoint) * airStrafeModifier);
        }
        // crouching
        if(currentState == MOVEMENT_STATE.GROUND && isCrouching)
        {
            wishVelocity = direction * crouchSpeed;
        }

        float yStore = velocity.y;
        velocity.y = 0f;
        wishVelocity.y = 0f;

        // acceleratin or not
        if (direction.magnitude > 0)
        {
            if(currentState == MOVEMENT_STATE.SLIDING)
            {
                float newVelocityLength = Vector3.Lerp(velocity, Vector3.zero, drag * delta).magnitude;
                Vector3 newVelocityDirection = Vector3.Lerp(velocity.normalized, wishVelocity.normalized, accel * delta);
                velocity = newVelocityDirection.normalized * newVelocityLength;
            }
            else
            {
                velocity = Vector3.Lerp(velocity, wishVelocity, accel * delta);
            }
        }
        else
        {
            velocity = Vector3.Lerp(velocity, wishVelocity, drag * delta);
        }
        velocity.y = yStore;
        if (gravityVec.y > 0.01f)
        {
            velocity.y = gravityVec.y;
        }
        else
        {
            velocity.y += gravityVec.y;
        }
        moveVelocity = velocity;
        controller.Move(velocity * delta);
    } 

    void ChangeState(MOVEMENT_STATE newState)
    {
        previousState = currentState;
        currentState = newState;
        if (previousState == MOVEMENT_STATE.SLIDING)
        {
            EndSlide?.Invoke();
        }

        if(currentState == MOVEMENT_STATE.SLIDING)
        {
            StartSlide?.Invoke();
        }

        if(currentState == MOVEMENT_STATE.WALLRUNNING)
        {
            StartWallRun?.Invoke(leftWallRun);
        }

        if(previousState == MOVEMENT_STATE.WALLRUNNING)
        {
            EndWallRun?.Invoke(leftWallRun);
        }
    }

    float GetHorizontalAngle(Vector3 v1, Vector3 v2)
    {
        v1.y = 0;
        v2.y = 0;

        return Mathf.Abs(Vector3.Angle(v1, v2));
    }

    void ApplyForce(Vector3 force)
    {
        moveVelocity += force;
    }

    IEnumerator ExecuteAfterTime(float time, Action callable)
    {
        yield return new WaitForSeconds(time);

        callable?.Invoke();
    }
    void ProcessMouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSense;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSense;

        transform.Rotate(Vector3.up * mouseX);
        //head.Rotate(Vector3.up * mouseX);
        xCameraRotation -= mouseY;
        xCameraRotation = Mathf.Clamp(xCameraRotation, -89.0f, 89.0f);
        head.localRotation = Quaternion.Euler(xCameraRotation, 0.0f, 0.0f);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(hit.normal.y > 0.1f)
        {
            floorNormal = hit.normal;
        }

        if (Mathf.Abs(hit.normal.y) < 0.1f)
        {
            if (!controller.isGrounded)
            {
                isOnWallOnly = true;
                wallNormal = hit.normal;

                lastWallCollisionPoint = hit.point;
            }
        }
        else
        {
            isOnWallOnly = false;
        }
    }

}
