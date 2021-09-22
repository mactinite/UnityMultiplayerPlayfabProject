using ECM.Controllers;
using MLAPI;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Messaging;
using UnityEngine.InputSystem;
using System;

public class FireProjectile : NetworkBehaviour
{

    public PlayerInput input;

    bool isAiming = false;
    public GameObject projectilePrefab;
    public float aimDistance = 200f;
    public float aimSensMultiplier = 0.75f;
    public BaseCharacterController controller;
    public GameObject characterModel;
    public Camera cam;
    public LayerMask hitMask;
    private Vector3 aimPosition;
    public Transform shootOrigin;
    private Quaternion aimRotation;

    private Quaternion originalRotation;
    private float aimValue = 0;
    public float rotationSmoothSpeed = 2f;
    public AnimationCurve rotationTransitionCurve;
    private Vector3 blockingAimPosition;
    private Vector3 _smoothedBlockedAimPosition;
    private Vector3 _smoothedAimPosition;
    private CameraFollow cameraFollow;
    private CinemachineBrain brain;
    private Vector3 reticleSmoothingVel;
    public Transform aimTransform;
    public Animator animator;
    public WeaponIK weaponIk;
    private Vector3 aimPosVel;
    private float aimPositionSmoothTime;

    private void Start()
    {
        cam = Camera.main;
        cameraFollow = Camera.main.GetComponent<CameraFollow>();
        brain = cam.GetComponent<CinemachineBrain>();
        weaponIk.enabled = false;
        input = cameraFollow.GetComponent<PlayerInput>();
        input.actions["Aim"].performed += AimStarted;
        input.actions["Aim"].canceled += AimCanceled;
        input.actions["Fire"].performed += FireWeapon;
        input.actions["Fire"].canceled += FireWeapon;
    }

    private void FireWeapon(InputAction.CallbackContext obj)
    {
        if (isAiming)
        {
            FireServerRpc(aimPosition, shootOrigin.transform.position, OwnerClientId);
        }
    }

    private void AimCanceled(InputAction.CallbackContext obj)
    {
        CameraFollow.Instance.aimCam.gameObject.SetActive(false);
        CameraFollow.Instance.blockingReticle.gameObject.SetActive(false);
        CameraFollow.Instance.reticle.gameObject.SetActive(false);
        isAiming = false;
    }

    private void AimStarted(InputAction.CallbackContext obj)
    {
        CameraFollow.Instance.aimCam.gameObject.SetActive(true);
        CameraFollow.Instance.reticle.gameObject.SetActive(true);

        isAiming = true;
    }

    private void Update()
    {
        if (IsOwner)
        {
   

            if (isAiming)
            {
                aimValue += Time.deltaTime * rotationSmoothSpeed;
                weaponIk.enabled = true;
                Aim();
            }
            else
            {
                aimValue -= Time.deltaTime * rotationSmoothSpeed;
                weaponIk.enabled = false;
            }

            aimValue = Mathf.Clamp(aimValue, 0, 1);
            _smoothedBlockedAimPosition = Vector3.SmoothDamp(_smoothedBlockedAimPosition, blockingAimPosition, ref reticleSmoothingVel, 0.1f);
            characterModel.transform.rotation = Quaternion.Slerp(transform.rotation, aimRotation, rotationTransitionCurve.Evaluate(aimValue));

            cameraFollow.blockingReticle.position = brain.OutputCamera.WorldToScreenPoint(_smoothedBlockedAimPosition);
            animator.SetBool("isAiming", isAiming);

        }
    }

    public override void NetworkStart()
    {
        cam = Camera.main;
    }

    public void Aim()
    {
        Vector3 screenPoint = cam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector3 rayOrigin = cam.ScreenToWorldPoint(screenPoint);
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, cam.transform.forward, out hit, aimDistance, hitMask))
        {
            aimPosition = hit.point;
        }
        else
        {
            aimPosition = cam.transform.position + cam.transform.forward * aimDistance;

        }

        _smoothedAimPosition = Vector3.SmoothDamp(aimTransform.position, aimPosition, ref aimPosVel, aimPositionSmoothTime);
        aimTransform.position = _smoothedAimPosition;

        // flattened direction hopefully
        Vector3 direction = (aimPosition - shootOrigin.transform.position);
        direction.y = 0;
        direction.Normalize();
        aimRotation = Quaternion.LookRotation(direction);

        RaycastHit aimHit;
        if (Physics.Raycast(shootOrigin.transform.position, shootOrigin.transform.forward, out aimHit, aimDistance, hitMask))
        {
            blockingAimPosition = aimHit.point;
            cameraFollow.blockingReticle.gameObject.SetActive(true);
        }
        else
        {
            cameraFollow.blockingReticle.gameObject.SetActive(false);
        }


    }


    

    [ServerRpc]
    public void FireServerRpc(Vector3 aimPos, Vector3 firePos, ulong clientId)
    {
        Vector3 position = firePos;
        Vector3 direction = aimPos - position;
        GameObject go = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction, Vector3.up));
        NetworkObject netObj = go.GetComponent<NetworkObject>();
        // give ownership to player who spawned the projectile to facilitate client side hit detection
        netObj.SpawnWithOwnership(clientId);
    }
}
