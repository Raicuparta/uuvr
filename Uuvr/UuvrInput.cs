using System.Runtime.InteropServices;

namespace Uuvr;

public class UuvrInput: UuvrBehaviour
{
    private enum XboxButton
    {
        DpadUp = 0x0001,
        DpadDown = 0x0002,
        DpadLeft = 0x0004,
        DpadRight = 0x0008,
        Start = 0x0010,
        Back = 0x0020,
        LeftThumb = 0x0040,
        RightThumb = 0x0080,
        LeftShoulder = 0x0100,
        RightShoulder = 0x0200,
        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000,
    }
    
    [DllImport("xinput1_4.dll", EntryPoint = "XInputSetButtonState")]
    private static extern void XInputSetButtonState(ushort wButton, bool bPressed);

    private static void SetButtonState(XboxButton button, bool pressed)
    {
        XInputSetButtonState((ushort) button, pressed);
    }

    private void Update()
    {
    }
}
