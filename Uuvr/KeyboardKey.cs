using System.Runtime.InteropServices;

namespace Uuvr;

// Even though Unity has its own Input stuff, there are multiple input systems that can be used,
// and different Unity versions have slightly different APIs for the same input system.
// So instead of relying on those systems, we just make our own using native system calls.
public class KeyboardKey
{
    public enum KeyCode
    {
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
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
