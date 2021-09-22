using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using ECM.Components;
using ECM.Controllers;
using mactinite.ToolboxCommons;
using UnityEngine.InputSystem;

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

    public PlayerInput input;
    private Vector2 mousePos = Vector2.zero;

    private void Awake()
    {
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
        SetCursorState(true);
        input.actions["Release Mouse"].performed += ReleaseCursor;
        input.actions["Release Mouse"].canceled += LockCursor;
    }

    private void ReleaseCursor(InputAction.CallbackContext obj)
    {
        SetCursorState(false);
    }

    private void LockCursor(InputAction.CallbackContext obj)
    {
        SetCursorState(true);
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

        if (followTarget != null && lockCursor)
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
        }

    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(true);
    }

    private void SetCursorState(bool newState)
    {
        lockCursor = newState;
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
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
        mousePos = input.actions["Look"].ReadValue<Vector2>() * Time.deltaTime;
        _look.x = mousePos.x * lateralSensitivity;
        _look.y = -(mousePos.y * verticalSensitivity);

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

    private void OnDrawGizmos()
    {
        if (Application.IsPlaying(this))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cameraTrackingTransform.position, 0.25f);
        }
    }

}
