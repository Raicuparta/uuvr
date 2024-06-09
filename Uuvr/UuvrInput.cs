// TODO: Emulate Input.

// using System.Runtime.InteropServices;
// using UnityEngine;
// using Valve.VR;
//
// namespace Uuvr;
//
// public class UuvrInput: UuvrBehaviour
// {
//     private enum XboxButton
//     {
//         DpadUp = 0x0001,
//         DpadDown = 0x0002,
//         DpadLeft = 0x0004,
//         DpadRight = 0x0008,
//         Start = 0x0010,
//         Back = 0x0020,
//         LeftThumb = 0x0040,
//         RightThumb = 0x0080,
//         LeftShoulder = 0x0100,
//         RightShoulder = 0x0200,
//         A = 0x1000,
//         B = 0x2000,
//         X = 0x4000,
//         Y = 0x8000,
//     }
//
//     private void Awake()
//     {
//         SteamVR.Initialize();
//     }
//
//     [DllImport("xinput1_4.dll", EntryPoint = "XInputSetButtonState")]
//     private static extern void XInputSetButtonState(ushort wButton, bool bPressed);
//     
//     [DllImport("xinput1_4.dll", EntryPoint = "XInputSetTriggerState")]
//     private static extern void XInputSetTriggerState(bool bLeft, byte bValue);
//     
//     [DllImport("xinput1_4.dll", EntryPoint = "XInputSetThumbState")]
//     private static extern void XInputSetThumbState(bool bLeft, short sX, short sY);
//
//     private static void SetButtonState(XboxButton button, bool pressed)
//     {
//         XInputSetButtonState((ushort) button, pressed);
//     }
//
//     private void Update()
//     {
//         var actions = SteamVR_Actions.Xbox;
//         // SetButtonState(XboxButton.DpadUp ,actions.DpadUp.state);
//         // SetButtonState(XboxButton.DpadDown ,actions.DpadDown.state);
//         // SetButtonState(XboxButton.DpadLeft ,actions.DpadLeft.state);
//         // SetButtonState(XboxButton.DpadRight ,actions.DpadRight.state);
//         SetButtonState(XboxButton.Start, actions.Start.state);
//         SetButtonState(XboxButton.Back, actions.Select.state);
//         SetButtonState(XboxButton.LeftThumb, actions.StickLeftClick.state);
//         SetButtonState(XboxButton.RightThumb, actions.StickRightClick.state);
//         SetButtonState(XboxButton.LeftShoulder, actions.LB.state);
//         SetButtonState(XboxButton.RightShoulder, actions.RB.state);
//         SetButtonState(XboxButton.A, actions.A.state);
//         SetButtonState(XboxButton.B, actions.B.state);
//         SetButtonState(XboxButton.X, actions.X.state);
//         SetButtonState(XboxButton.Y, actions.Y.state);
//
//         XInputSetTriggerState(true, (byte) (actions.LT.axis * 255));
//         XInputSetTriggerState(false, (byte) (actions.RT.axis * 255));
//         
//         XInputSetThumbState(true, (short) (actions.StickLeft.axis.x * short.MaxValue), (short) (actions.StickLeft.axis.y * short.MaxValue));
//         XInputSetThumbState(false, (short) (actions.StickRight.axis.x * short.MaxValue), (short) (actions.StickRight.axis.y * short.MaxValue));
//     }
// }
