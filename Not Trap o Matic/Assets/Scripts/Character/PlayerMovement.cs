using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 15.0f;
    public float gravity = 20.0f;
    public float jump = 12.0f;

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

    [Header("Air State")]
    const float airSnapLength = 0.1f;
    const float airAccel = 0.5f;
    const float airSpeed = 16.0f;
    const float airDrag = 0.1f;

    [Header("Air Strafing")]
    [SerializeField]
    public AnimationCurve airStrafeCurve;
    const float minStrafeAngle = 0.0f;
    const float maxStrafeAngle = 180.0f;
    const float airStrafeModifier = 1.0f;

    [Header("Jumping")]
    bool canJump = true;
    bool hasJumped = false;
    float coyoteTime = 0.2f;
    bool jumpQueued = false;

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
    [SerializeField]
    public AnimationCurve slideDragCurve;
    public AnimationCurve slopeAngleDragCurve;
    const float slideAccel = 0.8f;
    public float slideCurvePoint = 0.0f;
    const float slideDragTime = 0.6f;
    const float startSlideThreshold = 13.0f;
    const float endSlideSpeed = 11.0f;
    const float slideBoostForce = 4.0f;
    const float slideBoostTim = 2.0f;
    bool canSlideBoost = true;
    const float maxSLideSLopeSpeed = 25.0f;
    const float slideSlopeForce = 4.0f;
    
    [Header("Wall Running")]
    [SerializeField]
    public AnimationCurve wallrunCurve;
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
    public static event Action onJumped;
    public static event Action onLanded;
    public static event Action startSlide;
    public static event Action endSlide;
    public static event Action<bool> startWallRun;
    public static event Action<bool> endWallRun;

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

    int y = 0;
    // Update is called once per frame
    void Update()
    {
        ProcessMouseInput();

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


    private void FixedUpdate()
    {
        switch (currentState)
        {
            case (MOVEMENT_STATE.GROUND):
                Ground(Time.fixedDeltaTime);
                break;
        }
    }

    void Ground(float delta)
    {
        isCrouching = HandleCrouch(delta);

        float floor_snap_length = floorSnapLength;
        Vector3 gravity_vec = Vector3.zero;

        float velocity = controller.velocity.magnitude;
        //if (!isCrouching || (isCrouching && velocity > startSlideThreshold))
        //{
        //    Move(delta, floorAccel, floorDrag);
        //    if (isCrouching)
        //    {
        //        toSlide();
        //    }
        //}
        //else
        //{
        //    Move(delta, crouchAccel, floorDrag);
        //}

        //GroundToAir();
    }

    bool HandleCrouch(float delta, bool forceCrouch = false, bool forceUncrouch = false)
    {
        float height = 0.0f;
        bool crouching = Input.GetKey(KeyCode.LeftControl) || forceCrouch; // possible run to implement ceiling collision
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
}
