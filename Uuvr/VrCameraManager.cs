#if CPP
using System;
using UnhollowerBaseLib;
#endif
using UnityEngine;

namespace Uuvr;

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
        
        for (int index = 0; index < Camera.allCamerasCount; index ++)
        {
            Camera camera = _allCameras[index];
            if (camera == null || camera.targetTexture != null) continue;
            if (VrCamera.VrCameras.Contains(camera) || VrCamera.IgnoredCameras.Contains(camera)) continue;
            
            camera.gameObject.AddComponent<VrCamera>();
        }
    }
}
