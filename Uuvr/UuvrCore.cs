using System;
using System.Reflection;
using UnityEngine;
using Uuvr.OpenVR;
using Uuvr.VrCamera;
using Uuvr.VrTogglers;
using Uuvr.VrUi;

namespace Uuvr;

public class UuvrCore: MonoBehaviour
{
#if CPP
    public UuvrCore(IntPtr pointer) : base(pointer)
    {
    }
#endif

    private readonly KeyboardKey _toggleVrKey = new (KeyboardKey.KeyCode.F3);
    private float _originalFixedDeltaTime;
    
    private VrUiManager? _vrUi;
    private PropertyInfo? _refreshRateProperty;
    private VrTogglerManager? _vrTogglerManager;
    private KeyboardKey key = new KeyboardKey(KeyboardKey.KeyCode.F3);
    private KeyboardKey key2 = new KeyboardKey(KeyboardKey.KeyCode.F4);

    public static void Create()
    {
        new GameObject("UUVR").AddComponent<UuvrCore>();
    }

    private void SetUp()
    {
        if (gameObject.GetComponent<OpenVrManager>() == null)
        {
            gameObject.AddComponent<OpenVrManager>();
        }
    }

    private void OnDisable()
    {
        Debug.Log("UUVRCore Disabled, recreating");
        Destroy(gameObject);
        Create();
    }

    private void Update()
    {
        if (key.UpdateIsDown())
        {
            SetUp();
        }
        if (key2.UpdateIsDown())
        {
            Debug.Log("works");
        }
    }
}
