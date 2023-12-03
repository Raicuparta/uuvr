using System.Runtime.InteropServices;

namespace UnityUniversalVr;

public class KeyboardKey
{
    public enum KeyCode
    {
        F3 = 0x72,
        F4 = 0x73
    }
    
    private bool _previousIsKeyPressed;
    private bool _isKeyPressed;
    private readonly KeyCode _keyCode;

    public KeyboardKey(KeyCode keyCode)
    {
        _keyCode = keyCode;
    }
    
    public bool UpdateIsDown()
    {
        _previousIsKeyPressed = _isKeyPressed;
        _isKeyPressed = ((ushort)GetKeyState((int)_keyCode) & 0x8000) != 0;

        return !_previousIsKeyPressed && _isKeyPressed;
    }
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    private static extern short GetKeyState(int keyCode);
}
