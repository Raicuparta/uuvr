using UnityEngine;
using UnityEngine.XR;

namespace Uuvr;

public class UuvrPoseDriver: MonoBehaviour
{
    public static UuvrPoseDriver Create(Camera camera)
    {
        return camera.gameObject.AddComponent<UuvrPoseDriver>();
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
        transform.localRotation = InputTracking.GetLocalRotation(XRNode.CenterEye);
    }
}
