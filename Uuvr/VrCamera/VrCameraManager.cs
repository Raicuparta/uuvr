#if CPP
using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif
using UnityEngine;

namespace Uuvr.VrCamera;

public class VrCameraManager: MonoBehaviour
{
#if CPP
    public VrCameraManager(IntPtr pointer) : base(pointer)
    {
    }
#endif

#if CPP
    // At first this looks like it works with a Camera[],
    // but Camera.GetAllCameras just fills the array with nulls
    // unless we use Il2CppReferenceArray.
    private Il2CppReferenceArray<Camera> _allCameras;
#else
    private Camera[] _allCameras;
#endif
    
    private void Update()
    {
        if (_allCameras == null || _allCameras.Length < Camera.allCamerasCount)
        {
            _allCameras = new Camera[Camera.allCamerasCount];
        }
        Camera.GetAllCameras(_allCameras);
        
        for (var index = 0; index < Camera.allCamerasCount; index ++)
        {
            var camera = _allCameras[index];
            if (camera == null || camera.targetTexture != null || camera.stereoTargetEye == StereoTargetEyeMask.None) continue;
            if (VrCamera.VrCameras.Contains(camera) || VrCamera.IgnoredCameras.Contains(camera)) continue;
            
            Debug.Log($"creating vr camera {camera.name}");
            camera.gameObject.AddComponent<VrCamera>();
        }
    }
}
