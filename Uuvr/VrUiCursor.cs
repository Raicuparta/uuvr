using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Uuvr;

// TODO: add setting to toggle this.
public class VrUiCursor: MonoBehaviour
{
    public static void Create(Transform parent)
    {
        new GameObject(nameof(VrUiCursor))
        {
            transform =
            {
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity,
                parent = parent
            }
        }.AddComponent<VrUiCursor>();
    }
    
    private IEnumerator Start()
    {
        // I don't really know what I'm waiting for, but setting this too early made the cursor invisible.
        yield return new WaitForSeconds(3);
        
        // When I load the bmp like this and use it as a texture, it shows up upside down for some reason.
        // So I just flipped the cursor vertically in the actual bmp. Yeah dunno.
        var bitmap = new Bitmap(Path.Combine(UuvrPlugin.ModFolderPath, "Assets", "cursor.bmp"));
        var bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
        int numbytes = bmpdata.Stride * bitmap.Height;
        byte[] imageBytes = new byte[numbytes];
        var ptr = bmpdata.Scan0;
     
        Marshal.Copy(ptr, imageBytes, 0, numbytes);
     
        bitmap.UnlockBits(bmpdata);
        
        var texture = new Texture2D(48, 48, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(imageBytes);
        texture.Apply();
        
        Cursor.SetCursor(texture, new Vector2(2, 5), CursorMode.ForceSoftware);
    }
}