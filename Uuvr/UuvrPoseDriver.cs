using System;
using System.Reflection;
using UnityEngine;

namespace Uuvr;

public class UuvrPoseDriver: MonoBehaviour
{
    private MethodInfo _getLocalRotation;
    private readonly object[] getLocalRotationArgs = {
        2 // Enum value for XRNode.CenterEye
    };
    
    public static UuvrPoseDriver Create(Transform parent)
    {
        return new GameObject(nameof(UuvrPoseDriver))
        {
            transform =
            {
                parent = parent,
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity
            }
        }.AddComponent<UuvrPoseDriver>();
    }

    private void Awake()
    {
        Type inputTrackingType = Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.XRModule") ??
                                 Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.VRModule") ??
                                 Type.GetType("UnityEngine.VR.InputTracking, UnityEngine.VRModule") ??
                                 Type.GetType("UnityEngine.VR.InputTracking, UnityEngine");

        _getLocalRotation = inputTrackingType?.GetMethod("GetLocalRotation");

        if (_getLocalRotation == null)
        {
            Debug.LogError("Failed to find InputTracking.GetLocalRotation. Destroying UUVR Pose Driver.");
            Destroy(this);
        }

    }

    private void OnPreCull()
    {
        UpdateCamera();
    }

    private void OnPreRender()
    {
        UpdateCamera();
    }

    private void LateUpdate()
    {
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        transform.localRotation = (Quaternion)_getLocalRotation.Invoke(null, getLocalRotationArgs);
    }
}
