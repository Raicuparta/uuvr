using System;

namespace UnityEngine.XR.OpenXR
{
    /// <summary>
    /// Build time settings for OpenXR. These are serialized and available at runtime.
    /// </summary>
    [Serializable]
    public partial class OpenXRSettings : ScriptableObject
    {
        private static OpenXRSettings s_RuntimeInstance = null;

        private void Awake()
        {
            s_RuntimeInstance = this;
        }
        internal void ApplySettings()
        {
            ApplyRenderSettings();
        }

        private static OpenXRSettings GetInstance(bool useActiveBuildTarget)
        {
            var settings = s_RuntimeInstance;
            if (settings == null)
                settings = ScriptableObject.CreateInstance<OpenXRSettings>();

            return settings;
        }

        /// <summary>
        /// Accessor to OpenXR build time settings.
        ///
        /// In the Unity Editor, this returns the settings for the active build target group.
        /// </summary>
        public static OpenXRSettings ActiveBuildTargetInstance => GetInstance(true);

        /// <summary>
        /// Accessor to OpenXR build time settings.
        ///
        /// In the Unity Editor, this returns the settings for the Standalone build target group.
        /// </summary>
        public static OpenXRSettings Instance => GetInstance(false);
    }
}
