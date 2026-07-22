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

    // Start is called before the first frame update
    void Start()
    {
    }

    int x = 0;
    // Update is called once per frame
    void Update()
    {
        camera.fieldOfView = fov;
    }
}
