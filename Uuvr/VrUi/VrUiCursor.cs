using System.IO;
using UnityEngine;

namespace Uuvr.VrUi;

// The mouse cursor isn't visible in the VR UI plane, unless it's being rendered in software mode.
// So we use a custom mouse cursor graphic and render that.
public class VrUiCursor: UuvrBehaviour
{
#if CPP
    public VrUiCursor(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    private Texture2D _texture;
    private Vector2 _offset = new(22, 2);

    private void Start()
    {        
        var bytes = File.ReadAllBytes(Path.Combine(UuvrPlugin.ModFolderPath, @"Assets\cursor.bmp"));
        
        // Read dimensions from BMP header
        var width = bytes[18] + (bytes[19] << 8);
        var height = bytes[22] + (bytes[23] << 8);

        var colors = new Color32[width * height];
        _texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
        for (var i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color32(bytes[i * 4 + 54], bytes[i * 4 + 55], bytes[i * 4 + 56], bytes[i * 4 + 57]);
        }
        _texture.SetPixels32(colors);
        
        _texture.Apply();
    }

    private void Update()
    {
        if (_texture == null) return;

        // Perhaps it's unnecessary to set the cursor every frame,
        // but some games override it. I should probably leave it alone for games that already set it,
        // but I'm not sure how to check.
        Cursor.SetCursor(_texture, _offset, CursorMode.ForceSoftware);
    }
}