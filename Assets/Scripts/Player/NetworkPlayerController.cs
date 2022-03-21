using ECM.Controllers;
using MLAPI;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Messaging;
using UnityEngine.InputSystem;
using System;
using ECM.Examples;

public class NetworkPlayerController : NetworkBehaviour
{

    public PlayerInput input;

    public GameObject projectilePrefab;
    public float aimDistance = 200f;
    public float aimSensMultiplier = 0.75f;
    public CustomCharacterController controller;
    public GameObject characterModel;
    public Camera cam;
    public LayerMask hitMask;
    private Vector3 aimPosition;
    private Quaternion aimRotation;

    private bool networkStarted;
    private CameraFollow cameraFollow;
    private CinemachineBrain brain;
    
    private void LateUpdate()
    {
        if (IsLocalPlayer)
        {

            // flattened fwd camera direction
            Vector3 direction = cam.transform.forward;
            direction.y = 0;
            direction.Normalize();
            aimRotation = Quaternion.LookRotation(direction);

        }
    }

    public override void NetworkStart()
    {
        if (IsLocalPlayer)
        {
            cam = Camera.main;
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
            brain = cam.GetComponent<CinemachineBrain>();
            input = cameraFollow.GetComponent<PlayerInput>();
        
            CameraFollow.Instance.aimCam.gameObject.SetActive(false);
            networkStarted = true;
        }
    }

    private void Start()
    {
        if (!networkStarted)
        {
            cam = Camera.main;
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
            brain = cam.GetComponent<CinemachineBrain>();
            input = cameraFollow.GetComponent<PlayerInput>();

            CameraFollow.Instance.aimCam.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (IsLocalPlayer && input != null)
        {

        }
    }

    public void FaceAimDirection()
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
