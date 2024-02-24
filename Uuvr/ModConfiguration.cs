using System.ComponentModel;
using BepInEx.Configuration;

namespace Uuvr;

public class ModConfiguration
{
    public static ModConfiguration Instance;
    
    public enum CameraTracking
    {
        [Description("Absolute (takes over game camera)")]
        Absolute,
        [Description("Relative (adds to game camera)")]
        Relative,
    }

    public readonly ConfigEntry<CameraTracking> cameraTracking;
    public readonly ConfigEntry<bool> relativeCameraSetStereoView;

    public ModConfiguration(ConfigFile config)
    {
        Instance = this;
        
        cameraTracking = config.Bind(
            "General",
            nameof(cameraTracking),
            CameraTracking.Relative,
            "Defines how camera tracking is done. Relative is usually preferred, but not all games support it. Changing this might require restarting the level.");
        
        relativeCameraSetStereoView = config.Bind(
            "Relative Camera Tracking",
            nameof(relativeCameraSetStereoView),
            false,
            "Some games are better with this on, some are better with this off. Just try it and see which one is better. Changing this might require restarting the level.");
    }
}
