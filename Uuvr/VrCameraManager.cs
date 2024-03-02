#if CPP
using System;
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

    private Camera[] _allCameras;
    
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
            if (camera == null || !camera.stereoEnabled) continue;
            if (VrCamera.VrCameras.Contains(camera) || VrCamera.IgnoredCameras.Contains(camera)) continue;
            
            camera.gameObject.AddComponent<VrCamera>();
        }
    }
}
