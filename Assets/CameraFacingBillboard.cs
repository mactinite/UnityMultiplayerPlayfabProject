using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFacingBillboard : MonoBehaviour
{

	public Camera m_Camera;
	public bool autoInit = false;
	GameObject myContainer;

	void Awake()
	{
		if (autoInit == true)
		{
			m_Camera = Camera.main;
		}

	}

	//Orient the camera after all movement is completed this frame to avoid jittering
	void LateUpdate()
	{
			transform.LookAt(m_Camera.transform.position, m_Camera.transform.rotation * Vector3.up);
	}
}