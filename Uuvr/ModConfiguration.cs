using System.ComponentModel;
using BepInEx.Configuration;

namespace Uuvr;

public class ModConfiguration
{
    public static ModConfiguration Instance;
    
    public enum CameraTrackingMode
    {
        [Description("Absolute (moves/rotates existing camera)")]
        Absolute,
        [Description("Relative (changes existing camera rendering)")]
        Relative,
        [Description("Child (adds new camera)")]
        Child,
    }

#if MODERN
    public enum VrApi
    {
        [Description("OpenVR")]
        OpenVr,
        [Description("OpenXR")]
        OpenXr,
    }
#endif
    
    public enum ScreenSpaceCanvasType
    {
        [Description("None")]
        None,
        [Description("Not rendering to texture")]
        NotToTexture,
        [Description("All")]
        All,
    }

    public enum UiRenderMode
    {
        [Description("Overlay Camera (draws on top of everything)")]
        OverlayCamera,
        [Description("In World (can be occluded)")]
        InWorld,
    }

    public readonly ConfigFile Config;
    public readonly ConfigEntry<CameraTrackingMode> CameraTracking;
    public readonly ConfigEntry<bool> RelativeCameraSetStereoView;
    public readonly ConfigEntry<int> VrCameraDepth;
    public readonly ConfigEntry<int> VrUiLayerOverride;
    public readonly ConfigEntry<bool> AlignCameraToHorizon;
    public readonly ConfigEntry<bool> OverrideDepth;
    public readonly ConfigEntry<bool> PhysicsMatchHeadsetRefreshRate;
    public readonly ConfigEntry<bool> PatchUi;
    public readonly ConfigEntry<UiRenderMode> PreferredUiRenderMode;
    public readonly ConfigEntry<ScreenSpaceCanvasType> ScreenSpaceCanvasTypesToPatch;
    
#if MODERN
    public readonly ConfigEntry<VrApi> PreferredVrApi;
#endif

    public ModConfiguration(ConfigFile config)
    {
        Instance = this;

        Config = config;
        
#if MODERN
        PreferredVrApi = config.Bind(
            "General",
            "Preferred VR APi",
            VrApi.OpenXr,
            "VR API to use. Depending on the game, some APIs might be unavailable, so UUVR will fall back to one that works.");
#endif

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
            new ConfigDescription(
                "Requires enabling 'Override Depth'. Range is -100 to 100, but you should try to find the lowest value that fixes visibility.",
                new AcceptableValueRange<int>(-100, 100)));
        
        PhysicsMatchHeadsetRefreshRate = config.Bind(
            "General",
            "Force physics rate to match headset refresh rate",
            false,
            "Can help fix jiterriness in games that rely a lot on physics. Might break a lot of games too.");

        PatchUi = config.Bind(
            "UI",
            "Patch UI for VR",
            true,
            "Projects game UI on a plane in front of the VR camera");
        
        VrUiLayerOverride = config.Bind(
            "UI",
            "VR UI Layer Override",
            -1,
            new ConfigDescription(
                "Layer to use for VR UI. By default (value -1) UUVR falls back to an unused (unnamed) layer.",
                new AcceptableValueRange<int>(-1, 31)));

        ScreenSpaceCanvasTypesToPatch = config.Bind(
            "UI",
            "Screen-space UI elements to patch",
            ScreenSpaceCanvasType.NotToTexture,
            "Screen-space UI elements are already visible in VR with no patches. But in some games, they are difficult to see in VR. So you can choose to patch some (or all) of them to be rendered in the VR UI screen.");
        
        PreferredUiRenderMode = config.Bind(
            "UI",
            "Preferred UI Plane Render Mode",
            UiRenderMode.InWorld,
            "How to render the VR UI Plane. Overlay is usually better, but does't work in every game.");
    }
}
