using UnityEngine;

namespace Uuvr;

// Since changing camera pitch and roll in VR is more nauseating,
// we can use this to allow only yaw rotations, which preserve the horizon line.
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
            Vector3 forward = Vector3.ProjectOnPlane(transform.parent.forward, Vector3.up);
            transform.LookAt(transform.position + forward, Vector3.up);
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
