using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLine : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Debug.DrawLine(transform.position, transform.position + transform.forward * 50);
    }
}
