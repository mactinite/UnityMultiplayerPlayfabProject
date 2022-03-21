using Unity.Netcode;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using ECM.Examples;

public class NetworkPlayerController : NetworkBehaviour
{

    public PlayerInput input;
    public Camera cam;
    private bool networkStarted;
    private CameraFollow cameraFollow;
    
    private void LateUpdate()
    {
        if (IsLocalPlayer)
        {

            // flattened fwd camera direction
            Vector3 direction = cam.transform.forward;
            direction.y = 0;
            direction.Normalize();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            cam = Camera.main;
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
            input = cameraFollow.GetComponent<PlayerInput>();
            cameraFollow.followTarget = transform;
            CameraFollow.Instance.aimCam.gameObject.SetActive(false);
            networkStarted = true;
        }
    }

    private void Start()
    {
        if (IsLocalPlayer)
        {
            cam = Camera.main;
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
            input = cameraFollow.GetComponent<PlayerInput>();
            cameraFollow.followTarget = transform;
            CameraFollow.Instance.aimCam.gameObject.SetActive(false);
        }
    }

}
