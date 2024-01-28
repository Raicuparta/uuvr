using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace ShipbreakerVr;

public class VrCamera : MonoBehaviour
{
    public static void Create(Camera mainCamera)
    {
        var poseDriver = mainCamera.gameObject.AddComponent<TrackedPoseDriver>();
        poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
        poseDriver.rotationAction = new InputAction("VrRotation", InputActionType.PassThrough, "HeadTrackingOpenXR/centereyerotation");
    }
}
