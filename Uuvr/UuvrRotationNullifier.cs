using System;
using UnityEngine;

namespace Uuvr;

// Since changing camera pitch and roll in VR is more nauseating,
// we can use this to allow only yaw rotations, which preserve the horizon line.
public class UuvrRotationNullifier: UuvrBehaviour
{
#if CPP
    public UuvrRotationNullifier(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        ModConfiguration.Instance.AlignCameraToHorizon.SettingChanged += AlignToHorizonChanged;
    }

    private void OnDestroy()
    {
        ModConfiguration.Instance.AlignCameraToHorizon.SettingChanged -= AlignToHorizonChanged;
    }

    private void Start()
    {
        UpdateEnabledValue();
    }

    protected override void OnBeforeRender()
    {
        base.OnBeforeRender();
        UpdateTransform();
    }

    private void Update()
    {
        UpdateTransform();
    }
    
    private void LateUpdate()
    {
        UpdateTransform();
    }
    
    private void AlignToHorizonChanged(object sender, EventArgs e)
    {
        UpdateEnabledValue();
    }

    private void UpdateEnabledValue()
    {
        enabled = ModConfiguration.Instance.AlignCameraToHorizon.Value;
    }

    private void UpdateTransform()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.parent.forward, Vector3.up);
        transform.LookAt(transform.position + forward, Vector3.up);
    }
}
