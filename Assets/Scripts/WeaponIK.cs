using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HumanBone
{
    public HumanBodyBones bone;
    [Range(0, 1)]
    public float weight;
}

public class WeaponIK : MonoBehaviour
{
    public Transform targetTransform;
    public Transform aimTransform;
    public Transform bone;
    [Range(0,1)]
    public float weight = 1f;
    public int iterations = 10;
    public HumanBone[] humanBones;
    Transform[] boneTransforms;

    public float angleLimit = 90.0f;
    public float distanceLimit = 1.5f;
    void Start()
    {
        Animator animator = GetComponent<Animator>();
        boneTransforms = new Transform[humanBones.Length];
        for (int i = 0; i < boneTransforms.Length; i++)
        {
            boneTransforms[i] = animator.GetBoneTransform(humanBones[i].bone);
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 targetPosition = targetTransform.position;
        for (int i = 0; i < iterations; i++)
        {
            for (int b = 0; b < boneTransforms.Length; b++)
            {
                Transform bone = boneTransforms[b];
                float boneWeight = humanBones[b].weight * weight;
                AimAtTarget(bone, GetTargetPosition(), boneWeight);
            }
        }
    }

    Vector3 GetTargetPosition()
    {
        Vector3 targetDirection = targetTransform.position - aimTransform.position;
        Vector3 aimDirection = aimTransform.forward;
        float blendOut = 0f;

        float targetAngle = Vector3.Angle(targetDirection, aimDirection);
        if(targetAngle > angleLimit)
        {
            blendOut += (targetAngle - angleLimit) / 50.0f;
        }

        float targetDistance = targetDirection.magnitude;
        if(targetDistance < distanceLimit)
        {
            blendOut += distanceLimit - targetDistance;
        }

        Vector3 direction = Vector3.Slerp(targetDirection, aimDirection, blendOut);
        return aimTransform.position + direction;
    }
    private void AimAtTarget(Transform bone, Vector3 targetPosition, float boneWeight)
    {
        Vector3 aimDirection = aimTransform.forward;
        Vector3 targetDirection = targetPosition - aimTransform.position;
        Quaternion aimTowards = Quaternion.FromToRotation(aimDirection, targetDirection);
        Quaternion blendedRotation = Quaternion.Slerp(Quaternion.identity, aimTowards, boneWeight);
        bone.rotation = blendedRotation * bone.rotation;
    }
}
