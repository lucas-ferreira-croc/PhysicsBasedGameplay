using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Range(1.0f, 179.0f)]
    public float runFov = 98.0f;
    public float fov;

    [SerializeField]
    Camera camera;

    public PlayerMovement player;

    Vector3 jumpRotation = new Vector3(-1.0f, -0.5f, 0.2f);
    private ProceduralCurve<Vector3> jumpAnimation = new ProceduralCurve<Vector3>(new AnimationCurve(
      new Keyframe(0f, 0f, 0f, 0f),
      new Keyframe(0.0879121f, 1f, 0f, 0f),
      new Keyframe(0.568421f, -0.340659f, 0f, 0f),
      new Keyframe(1f, 1.49012e-08f, 0f, 0f)
    ));

    Vector3 landPosition = new Vector3(0.0f, -0.15f, 0.0f);
    private ProceduralCurve<Vector3> landPosAnimation = new ProceduralCurve<Vector3>(new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.0879121f, 1f, 0f, 0f),
        new Keyframe(0.568421f, -0.340659f, 0f, 0f),
        new Keyframe(1f, 1.49012e-08f, 0f, 0f)
    ));

    Vector3 landRotation = new Vector3(-0.5f, 0.0f, 0.0f);
    private ProceduralCurve<Vector3> landRotationAnimation = new ProceduralCurve<Vector3>(new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.44444f),
        new Keyframe(1f, 1f, 0.3f, 0f)
    ));

    float slideTilt = -1.0f;
    ProceduralCurve<float> slideAnimation = new ProceduralCurve<float>(new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 1.01754f),
        new Keyframe(1f, 1f, 1.0f, 0f)     
    ));

    float wallRunTilt = 31.0f;
    ProceduralCurve<float> wallRunAnimation = new ProceduralCurve<float>(new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.44444f),
        new Keyframe(1f, 1f, 0.3f, 0f)     
    ));

    ProceduralCurve<Vector3>[] rotationAnimations;
    ProceduralCurve<Vector3>[] positionAnimations;
    ProceduralCurve<float>[] tiltAnimations;

    private Vector3 currentAnimRotation = Vector3.zero;
    private Vector3 currentAnimPosition = Vector3.zero;
    private float currentAnimTilt = 0f;

    private void Awake()
    {
        rotationAnimations = new ProceduralCurve<Vector3>[]
        {
            jumpAnimation,
            landRotationAnimation
        };

        positionAnimations = new ProceduralCurve<Vector3>[]
        {
            landPosAnimation
        };

        tiltAnimations = new ProceduralCurve<float>[]
        {
            slideAnimation, wallRunAnimation
        };
    }

    // Start is called before the first frame update
    void Start()
    {
        PlayerMovement.JustJumped += StartJumpAnimation;
        PlayerMovement.JustLanded += StartLandedAnimation;
        PlayerMovement.StartSlide += StarSlide;
        PlayerMovement.EndSlide += EndSlide;
        PlayerMovement.StartWallRun += StartWallRun;
        PlayerMovement.EndWallRun += EndWallRun;

        jumpAnimation.length = 0.8f;
        landPosAnimation.length = 0.8f;
        landRotationAnimation.length = 0.5f;
        slideAnimation.length = 0.2f;
        wallRunAnimation.length = 0.2f;

        jumpAnimation.SetTargets(Vector3.zero, jumpRotation, Vector3.zero);
        landPosAnimation.SetTargets(Vector3.zero, landPosition, Vector3.zero);
        landRotationAnimation.SetTargets(Vector3.zero, landRotation, Vector3.zero);
        slideAnimation.SetTargets(0.0f, slideTilt, slideTilt);
        wallRunAnimation.SetTargets(0.0f, wallRunTilt, wallRunTilt);
    }

    private void FixedUpdate()
    {
        //currentAnimRotation = Vector3.zero;
        //currentAnimPosition = Vector3.zero;
        //currentAnimTilt = 0f;

        //foreach (var animation in rotationAnimations)
        //{
        //    if(animation.IsRunning())
        //    {
        //        currentAnimRotation += animation.Step(Time.fixedDeltaTime);
        //    }
        //}

        //foreach(var animation in positionAnimations)
        //{
        //    if (animation.IsRunning())
        //    {
        //        currentAnimPosition += animation.Step(Time.fixedDeltaTime);
        //    }
        //}

        //foreach (var animation in tiltAnimations)
        //{
        //    if (animation.IsRunning())
        //    {
        //        currentAnimTilt += animation.Step(Time.fixedDeltaTime);
        //    }
        //}

        //transform.localPosition = currentAnimPosition;
        //Quaternion additiveRotation = Quaternion.Euler(currentAnimRotation.x, currentAnimRotation.y, currentAnimRotation.z + currentAnimTilt);
        //transform.localRotation = transform.localRotation * additiveRotation;
    }

    // Update is called once per frame
    void Update()
    {
        camera.fieldOfView = fov;
    }

    void StartJumpAnimation()
    {
        landPosAnimation.ForceStop();
        landRotationAnimation.ForceStop();
        jumpAnimation.Initialize(Vector3.zero);
    }

    void StartLandedAnimation()
    {
        jumpAnimation.ForceStop();
        landPosAnimation.Initialize(Vector3.zero);
        jumpAnimation.Initialize(Vector3.zero);
    }

    void StarTilt(ProceduralCurve<Vector3> animation)
    {
        foreach(var anim in rotationAnimations)
        {
            anim.ForceStop();
        }

        animation.Initialize(transform.localEulerAngles);
    }

    void StarTilt(ProceduralCurve<float> animation)
    {
        foreach (var anim in rotationAnimations)
        {
            anim.ForceStop();
        }

        animation.Initialize(transform.localEulerAngles.z);
    }

    void EndTilt(ProceduralCurve<Vector3> animation)
    {
        foreach(var anim in rotationAnimations)
        {
            if (anim.IsRunning())
            {
            }
        }

        animation.InitializeBackwards(transform.localEulerAngles);
    }
    void EndTilt(ProceduralCurve<float> animation)
    {
        foreach (var anim in rotationAnimations)
        {
            if (anim.IsRunning())
            {
            }
        }

        animation.InitializeBackwards(transform.localEulerAngles.z);
    }

    void StartWallRun(bool left)
    {
        wallRunTilt = (wallRunTilt < 0 && !left) || (wallRunTilt > 0 && left) ? -wallRunTilt : wallRunTilt;
        wallRunAnimation.targets.max = wallRunTilt;
        wallRunAnimation.targets.snap = wallRunTilt;
        StarTilt(wallRunAnimation);
    }

    void EndWallRun(bool left)
    {
        EndTilt(wallRunAnimation);
    }

    void StarSlide()
    {
        StarTilt(slideAnimation);
    }

    void EndSlide()
    {
        EndTilt(slideAnimation);
    }
}
