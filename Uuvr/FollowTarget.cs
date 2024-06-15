using UnityEngine;

namespace Uuvr;

public class FollowTarget: UuvrBehaviour
{
#if CPP
    public FollowTarget(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    public Transform? Target;
    public Vector3 LocalPosition = Vector3.zero;
    public Quaternion LocalRotation = Quaternion.identity;

    protected override void OnBeforeRender()
    {
        if (Target == null) return;

        transform.position = Target.TransformPoint(LocalPosition);
        transform.rotation = Target.rotation * LocalRotation;
    }
}
