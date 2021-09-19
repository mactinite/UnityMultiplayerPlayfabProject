using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using ECM.Components;
using ECM.Controllers;
using mactinite.ToolboxCommons;
public class CameraFollow : SingletonMonobehavior<CameraFollow>
{
    public Transform followTarget;
    public float lateralSensitivity = 5;
    public float verticalSensitivity = 5;

    private Transform cameraTrackingTransform;
    private GroundDetection groundDetection;

    public CinemachineVirtualCamera followCam;
    public CinemachineVirtualCamera aimCam;
    public RectTransform reticle;
    public RectTransform blockingReticle;
    public float smoothTime = 1f;
    public float rotationSmoothTime = 1f;

    public float maxYGain = 5f;
    private float lastYPosition;
    private Vector3 _vel;


    private Vector2 _look = Vector2.zero;
    private float rotationPower = 3f;
    private bool lockCursor = false;


    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        if (cameraTrackingTransform == null)
        {
            cameraTrackingTransform = new GameObject("Camera Target").transform;
            followCam.Follow = cameraTrackingTransform;
            followCam.LookAt = cameraTrackingTransform;
            aimCam.Follow = cameraTrackingTransform;
            aimCam.LookAt = cameraTrackingTransform;
        }

        aimCam.gameObject.SetActive(false);
        blockingReticle.gameObject.SetActive(false);
        reticle.gameObject.SetActive(false);
    }

    private void Update()
    {

        if(cameraTrackingTransform == null)
        {
            cameraTrackingTransform = new GameObject("Camera Target").transform;
            followCam.Follow = cameraTrackingTransform;
            followCam.LookAt = cameraTrackingTransform;
            aimCam.Follow = cameraTrackingTransform;
            aimCam.LookAt = cameraTrackingTransform;
        }

        if (followTarget != null)
        {
            if (!groundDetection)
            {
                groundDetection = followTarget.GetComponent<GroundDetection>();
            }

            var desiredPosition = followTarget.position;

            if (!groundDetection.isOnGround && followTarget.position.y > lastYPosition)
            {
                desiredPosition = new Vector3(followTarget.position.x, lastYPosition, followTarget.position.z);
            }
            else
            {
                lastYPosition = followTarget.position.y;
            }

            cameraTrackingTransform.position = Vector3.SmoothDamp(cameraTrackingTransform.position, desiredPosition, ref _vel, smoothTime);
            Rotation();
            if (lockCursor == false)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                lockCursor = true;
            }
        }
        else
        {
            if (lockCursor == true)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                lockCursor = false;
            }
        }

        if (lockCursor == true && Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }


    public void LockCursor()
    {
        lockCursor = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        lockCursor = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Rotation()
    {
        _look.x = Input.GetAxis("Mouse X") * lateralSensitivity;
        _look.y = -(Input.GetAxis("Mouse Y") * verticalSensitivity);

        cameraTrackingTransform.transform.rotation *= Quaternion.AngleAxis(_look.x * rotationPower, Vector3.up);

        cameraTrackingTransform.transform.rotation *= Quaternion.AngleAxis(_look.y * rotationPower, Vector3.right);


        var angles = cameraTrackingTransform.localEulerAngles;
        angles.z = 0;

        var angle = cameraTrackingTransform.localEulerAngles.x;

        //Clamp the Up/Down rotation
        if (angle > 180 && angle < 340)
        {
            angles.x = 340;
        }
        else if (angle < 180 && angle > 40)
        {
            angles.x = 40;
        }


        cameraTrackingTransform.localEulerAngles = angles;
    }

}
