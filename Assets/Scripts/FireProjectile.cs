using ECM.Controllers;
using MLAPI;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Messaging;

public class FireProjectile : NetworkBehaviour
{
    bool isAiming = false;
    public GameObject projectilePrefab;
    public float aimDistance = 200f;
    public float aimSensMultiplier = 0.75f;
    public BaseCharacterController controller;
    public GameObject characterModel;
    public Camera camera;
    public LayerMask hitMask;
    private Vector3 aimPosition;
    public Transform shootOrigin;
    private Quaternion aimRotation;

    private Quaternion originalRotation;
    private float aimValue = 0;
    public float rotationSmoothSpeed = 2f;
    public AnimationCurve rotationTransitionCurve;
    private Vector3 blockingAimPosition;
    private Vector3 _smoothedAimPosition;
    private CameraFollow cameraFollow;
    private CinemachineBrain brain;
    private Vector3 reticleSmoothingVel;

    private void Start()
    {
        camera = Camera.main;
        cameraFollow = CameraFollow.Instance;
        brain = camera.GetComponent<CinemachineBrain>();
    }

    private void Update()
    {
        if (IsLocalPlayer || IsOwner)
        {
            if (Input.GetMouseButtonDown(1))
            {
                CameraFollow.Instance.aimCam.gameObject.SetActive(true);
                CameraFollow.Instance.reticle.gameObject.SetActive(true);

                isAiming = true;
            }
            if (Input.GetMouseButton(1))
            {
                Aim();
            }
            if (Input.GetMouseButtonUp(1))
            {
                CameraFollow.Instance.aimCam.gameObject.SetActive(false);
                CameraFollow.Instance.blockingReticle.gameObject.SetActive(false);
                CameraFollow.Instance.reticle.gameObject.SetActive(false);
                isAiming = false;
            }


            if (isAiming)
            {
                aimValue += Time.deltaTime * rotationSmoothSpeed;
            } else
            {
                aimValue -= Time.deltaTime* rotationSmoothSpeed;
            }

            aimValue = Mathf.Clamp(aimValue, 0, 1);
            characterModel.transform.rotation = Quaternion.Slerp(transform.rotation, aimRotation, rotationTransitionCurve.Evaluate(aimValue));
            _smoothedAimPosition = Vector3.SmoothDamp(_smoothedAimPosition, blockingAimPosition, ref reticleSmoothingVel, 0.1f);
            cameraFollow.blockingReticle.position = brain.OutputCamera.WorldToScreenPoint(_smoothedAimPosition);


        }
    }

    public override void NetworkStart()
    {
        camera = Camera.main;
    }

    public void Aim()
    {
        Vector3 screenPoint = camera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector3 rayOrigin = camera.ScreenToWorldPoint(screenPoint);
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, camera.transform.forward, out hit, aimDistance, hitMask))
        {
            aimPosition = hit.point;
         

        }
        else
        {
            aimPosition = camera.transform.position + camera.transform.forward * aimDistance;
        }



        aimRotation = Quaternion.LookRotation(aimPosition - shootOrigin.transform.position);


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

        if (Input.GetMouseButtonDown(0))
        {
            FireServerRpc(aimPosition, shootOrigin.transform.position, OwnerClientId);
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
