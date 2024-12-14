using System;
using BepInEx.Configuration;

namespace Uuvr.VrTogglers;

public class VrTogglerManager
{
    private VrToggler _toggler;
    
    public VrTogglerManager()
    {
        SetUpToggler();
    }

    private void SetUpToggler()
    {
        if (_toggler != null)
        {
            _toggler.SetVrEnabled(false);
        }
        
#if MODERN && MONO
        // TODO: should never pick OpenXR on x86, since it no worky.
        switch(ModConfiguration.Instance.PreferredVrApi.Value)
        {
            case ModConfiguration.VrApi.OpenVr:
            {
                _toggler = new XrPluginOpenVrToggler();
                return;
            }
            case ModConfiguration.VrApi.OpenXr:
            {
                _toggler = new XrPluginOpenXrToggler();
                return;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
#elif LEGACY && MONO
        _toggler = new NativeOpenVrToggler();
#else
        _toggler = new ManualOpenVrToggler();
#endif
    }

    public void ToggleVr()
    {
        _toggler.SetVrEnabled(!_toggler.IsVrEnabled);
    }
}
