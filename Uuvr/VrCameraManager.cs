using UnityEngine;

namespace Uuvr;

public class VrCameraManager: MonoBehaviour
{
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
            if (!camera.stereoEnabled) continue;
            if (VrCamera.VrCameras.Contains(camera) || VrCamera.IgnoredCameras.Contains(camera)) continue;

            camera.gameObject.AddComponent<VrCamera>();
        }
    }
}
