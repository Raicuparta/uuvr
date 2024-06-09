using System;
using UnityEngine;

namespace Uuvr.VrCamera;

// TODO: add manual offsets.
public class VrCameraOffset: UuvrBehaviour
{
#if CPP
    public VrCameraOffset(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    protected override void OnBeforeRender()
    {
        base.OnBeforeRender();
        UpdateTransform();
    }

    protected override void OnSettingChanged()
    {
        base.OnSettingChanged();
        transform.localPosition = ModConfiguration.Instance.CameraPositionOffset.Value;
    }

    private void Update()
    {
        UpdateTransform();
    }
    
    private void LateUpdate()
    {
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        if (ModConfiguration.Instance.AlignCameraToHorizon.Value)
        {
            var forward = Vector3.ProjectOnPlane(transform.parent.forward, Vector3.up);
            transform.LookAt(transform.position + forward, Vector3.up);
        }
        else
        {
            transform.localRotation = Quaternion.identity;
        }
    }
}
