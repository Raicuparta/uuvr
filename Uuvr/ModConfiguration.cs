using System.ComponentModel;
using BepInEx.Configuration;

namespace Uuvr;

public class ModConfiguration
{
    public static ModConfiguration? Instance;
    
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
    public readonly ConfigEntry<bool> AlignCameraToHorizon;
    public readonly ConfigEntry<bool> OverrideDepth;

    public ModConfiguration(ConfigFile config)
    {
        Instance = this;
        
        CameraTracking = config.Bind(
            "Camera",
            "Camera Tracking Mode",
            CameraTrackingMode.Relative,
            "Defines how camera tracking is done. Relative is usually preferred, but not all games support it. Changing this might require restarting the level.");
        
        RelativeCameraSetStereoView = config.Bind(
            "Relative Camera",
            "Use SetStereoView for Relative Camera",
            false,
            "Some games are better with this on, some are better with this off. Just try it and see which one is better. Changing this might require restarting the level.");
        
        AlignCameraToHorizon = config.Bind(
            "Camera",
            "Align To Horizon",
            false,
            "Prevents pitch and roll changes on the camera, allowing only yaw changes.");
        
        OverrideDepth = config.Bind(
            "Camera",
            "Override Depth",
            false,
            "In some games, the VR camera won't display anything unless we override the camera depth value.");
        
        VrCameraDepth = config.Bind(
            "Camera Depth",
            "Depth Value",
            1,
            "Requires enabling 'Override Depth'. Range is -100 to 100, but you should try to find the lowest value that fixes visibility.");
    }
}
