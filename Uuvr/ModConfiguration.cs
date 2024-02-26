using System.ComponentModel;
using BepInEx.Configuration;

namespace Uuvr;

public class ModConfiguration
{
    public static ModConfiguration Instance;
    
    public enum CameraTrackingMode
    {
        [Description("Absolute (takes over game camera)")]
        Absolute,
        [Description("Relative (adds to game camera)")]
        Relative,
    }

    public readonly ConfigEntry<CameraTrackingMode> CameraTracking;
    public readonly ConfigEntry<bool> RelativeCameraSetStereoView;
    public readonly ConfigEntry<int> VrCameraDepth;

    public ModConfiguration(ConfigFile config)
    {
        Instance = this;
        
        CameraTracking = config.Bind(
            "General",
            "Camera Tracking Mode",
            CameraTrackingMode.Relative,
            "Defines how camera tracking is done. Relative is usually preferred, but not all games support it. Changing this might require restarting the level.");
        
        RelativeCameraSetStereoView = config.Bind(
            "Relative Camera",
            "Use SetStereoView for Relative Camera.",
            false,
            "Some games are better with this on, some are better with this off. Just try it and see which one is better. Changing this might require restarting the level.");
        
        VrCameraDepth = config.Bind(
            "General",
            "VR Camera Depth",
            1,
            "In some games, the VR camera won't display anything unless we increase this number. Range is -100 to 100, but you should try to find the lowest value that fixes visibility.");
    }
}
