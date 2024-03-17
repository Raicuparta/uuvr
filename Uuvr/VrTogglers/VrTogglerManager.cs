using System;
using BepInEx.Configuration;

namespace Uuvr.VrTogglers;

public class VrTogglerManager
{
    private VrToggler _toggler;
    
    public VrTogglerManager()
    {
        SetUpToggler();
        _toggler?.SetVrEnabled(true);
    }

    private void SetUpToggler()
    {
        if (_toggler != null)
        {
            _toggler.SetVrEnabled(false);
        }
        
#if MODERN
        switch(ModConfiguration.Instance.PreferredVrApi.Value)
        {
            case ModConfiguration.VrApi.OpenVR:
            {
                _toggler = new XrPluginOpenVrToggler();
                return;
            }
            case ModConfiguration.VrApi.OpenXR:
            {
                _toggler = new XrPluginOpenXrToggler();
                return;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
#else
        _toggler = new LegacyOpenVrToggler();
#endif
    }

    public void ToggleVr()
    {
        _toggler.SetVrEnabled(!_toggler.IsVrEnabled);
    }
}
