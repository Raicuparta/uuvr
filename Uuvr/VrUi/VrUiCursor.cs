using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Uuvr.VrUi;

// The mouse cursor isn't visible in the VR UI plane, unless it's being rendered in software mode.
// So we use a custom mouse cursor graphic and render that.
// TODO: Look into games that already do this, and possibly revert it to hardware mouse.
// Might need to patch the cursor methods to prevent it.
public class VrUiCursor: UuvrBehaviour
{
#if CPP
    public VrUiCursor(IntPtr pointer) : base(pointer)
    {
    }
#endif

    private Texture2D _texture;

    private void Start()
    {
        StartCoroutine(SetUpCursor());
    }

    private IEnumerator SetUpCursor()
    {
        // I don't really know what I'm waiting for, but setting this too early made the cursor invisible.
        yield return new WaitForSeconds(3);
        
        // When I load the bmp like this and use it as a texture, it shows up upside down for some reason.
        // So I just flipped the cursor vertically in the actual bmp. Yeah dunno.
        // TODO: this stuff does't exist in a bunch of games, try something else for loading BMP.
        var bitmap = new Bitmap(Path.Combine(UuvrPlugin.ModFolderPath, @"Assets\cursor.bmp"));
        var bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
        var numbytes = bmpdata.Stride * bitmap.Height;
        var imageBytes = new byte[numbytes];
        var ptr = bmpdata.Scan0;
     
        Marshal.Copy(ptr, imageBytes, 0, numbytes);
     
        bitmap.UnlockBits(bmpdata);
        
        _texture = new Texture2D(48, 48, TextureFormat.RGBA32, false);
        _texture.LoadRawTextureData(imageBytes);
        _texture.Apply();
    }

    private void Update()
    {
        if (_texture == null) return;

        Cursor.SetCursor(_texture, new Vector2(2, 5), CursorMode.ForceSoftware);
    }
}