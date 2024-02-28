using UnityEngine;

namespace Uuvr;

// Since changing camera pitch and roll in VR is more nauseating,
// we can use this to allow only yaw rotations, which preserve the horizon line.
// TODO: add config toggle for this.
// TODO: Figure out better way to run update, to prevent it from being jittery.
public class UuvrRotationNullifier: MonoBehaviour
{
    public static UuvrRotationNullifier Create(Transform parent)
    {
        return new GameObject(nameof(UuvrPoseDriver))
        {
            transform =
            {
                parent = parent,
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity
            }
        }.AddComponent<UuvrRotationNullifier>();
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
        Vector3 forward = Vector3.ProjectOnPlane(transform.parent.forward, Vector3.up);
        transform.LookAt(transform.position + forward, Vector3.up);
    }
}
