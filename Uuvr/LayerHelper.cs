using UnityEngine;

namespace Uuvr;

public static class LayerHelper
{
    private static int _freeLayer = -1;
    
    // Unity only lets you define 32 layers.
    // This is annoying because it's useful for us to create layers for some VR-specific stuff.
    // We try to find a free layer (one without a name), but some games use all 32 layers.
    // In that case, we need to fall back to something else.
    private static int FindFreeLayer()
    {
        for (var layer = 31; layer >= 0; layer--)
        {
            if (LayerMask.LayerToName(layer).Length != 0) continue;

            Debug.Log($"Found free layer: {layer}");
            return layer;
        }

        Debug.LogWarning("Failed to find a free layer to use for VR UI. Falling back to last layer.");
        return 31;
    }

    private static int GetFreeLayerCached()
    {
        if (_freeLayer == -1)
        {
            _freeLayer = FindFreeLayer();
        }

        return _freeLayer;
    }

    public static int GetVrUiLayer()
    {
        var layerOverride = ModConfiguration.Instance.VrUiLayerOverride.Value;
        return layerOverride > -1 ? layerOverride : GetFreeLayerCached();
    }

    public static void SetLayerRecursive(Transform transform, int layer)
    {
        // Not using the usual foreach Transform etc because it fails in silly il2cpp.
        for (var index = 0; index < transform.childCount; index++)
        {
            var child = transform.GetChild(index);
            SetLayerRecursive(child, layer);
            transform.gameObject.layer = layer;
        }
    }
}
