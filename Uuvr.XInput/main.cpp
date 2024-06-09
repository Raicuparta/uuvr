#include <windows.h>

#define XINPUT_GAMEPAD_DPAD_UP          0x0001
#define XINPUT_GAMEPAD_DPAD_DOWN        0x0002
#define XINPUT_GAMEPAD_DPAD_LEFT        0x0004
#define XINPUT_GAMEPAD_DPAD_RIGHT       0x0008
#define XINPUT_GAMEPAD_START            0x0010
#define XINPUT_GAMEPAD_BACK             0x0020
#define XINPUT_GAMEPAD_LEFT_THUMB       0x0040
#define XINPUT_GAMEPAD_RIGHT_THUMB      0x0080
#define XINPUT_GAMEPAD_LEFT_SHOULDER    0x0100
#define XINPUT_GAMEPAD_RIGHT_SHOULDER   0x0200
#define XINPUT_GAMEPAD_A                0x1000
#define XINPUT_GAMEPAD_B                0x2000
#define XINPUT_GAMEPAD_X                0x4000
#define XINPUT_GAMEPAD_Y				0x8000

#define BATTERY_TYPE_DISCONNECTED		0x00

#define XUSER_MAX_COUNT                 4
#define XUSER_INDEX_ANY					0x000000FF

#define ERROR_DEVICE_NOT_CONNECTED		1167
#define ERROR_SUCCESS					0

typedef struct _XINPUT_GAMEPAD
{
	WORD                                wButtons;
	BYTE                                bLeftTrigger;
	BYTE                                bRightTrigger;
	SHORT                               sThumbLX;
	SHORT                               sThumbLY;
	SHORT                               sThumbRX;
	SHORT                               sThumbRY;
} XINPUT_GAMEPAD, *PXINPUT_GAMEPAD;

typedef struct _XINPUT_STATE
{
	DWORD                               dwPacketNumber;
	XINPUT_GAMEPAD                      Gamepad;
} XINPUT_STATE, *PXINPUT_STATE;

typedef struct _XINPUT_VIBRATION
{
	WORD                                wLeftMotorSpeed;
	WORD                                wRightMotorSpeed;
} XINPUT_VIBRATION, *PXINPUT_VIBRATION;

typedef struct _XINPUT_CAPABILITIES
{
	BYTE                                Type;
	BYTE                                SubType;
	WORD                                Flags;
	XINPUT_GAMEPAD                      Gamepad;
	XINPUT_VIBRATION                    Vibration;
} XINPUT_CAPABILITIES, *PXINPUT_CAPABILITIES;

typedef struct _XINPUT_BATTERY_INFORMATION
{
	BYTE BatteryType;
	BYTE BatteryLevel;
} XINPUT_BATTERY_INFORMATION, *PXINPUT_BATTERY_INFORMATION;

typedef struct _XINPUT_KEYSTROKE
{
	WORD    VirtualKey;
	WCHAR   Unicode;
	WORD    Flags;
	BYTE    UserIndex;
	BYTE    HidCode;
} XINPUT_KEYSTROKE, *PXINPUT_KEYSTROKE;

#define DLLEXPORT extern "C" __declspec(dllexport)

DLLEXPORT BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	/*switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}*/
	return TRUE;
}

static XINPUT_STATE state = { 0 };


DLLEXPORT DWORD WINAPI XInputGetState(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE *pState)
{
	// test by setting button A to pressed:
	// XInputSetButtonState(XINPUT_GAMEPAD_A, true);

	// 	pState->Gamepad.bRightTrigger = 0;
	// pState->Gamepad.bLeftTrigger = 0;
	// pState->Gamepad.sThumbLX = 0;
	// pState->Gamepad.sThumbLY = 0;
	// pState->Gamepad.sThumbRX = 0;
	// pState->Gamepad.sThumbRY = 0;
	// pState->Gamepad.wButtons = 0;
	//
	// if ((GetAsyncKeyState(VK_SPACE) & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_A;
	// if ((GetAsyncKeyState('E') & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_X;
	// if ((GetAsyncKeyState('Q') & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_Y;
	// if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_B;
	//
	// if ((GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0) pState->Gamepad.bRightTrigger = 255; 
	// if ((GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0) pState->Gamepad.bLeftTrigger = 255;
	//
	// if ((GetAsyncKeyState(VK_MBUTTON) & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_THUMB;
	//
	// if ((GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_THUMB;
	// if ((GetAsyncKeyState('1') & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_SHOULDER;
	// if ((GetAsyncKeyState('2') & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_SHOULDER;
	//
	// if ((GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_BACK;
	// if ((GetAsyncKeyState(VK_RETURN) & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_START;
	//
	// if ((GetAsyncKeyState(VK_UP) & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
	// if ((GetAsyncKeyState(VK_DOWN) & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
	// if ((GetAsyncKeyState(VK_LEFT) & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
	// if ((GetAsyncKeyState(VK_RIGHT) & 0x8000) != 0) pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
	//
	// if ((GetAsyncKeyState('T') & 0x8000) != 0) pState->Gamepad.sThumbLY = 32767;
	// if ((GetAsyncKeyState('G') & 0x8000) != 0) pState->Gamepad.sThumbLY = -32768;
	// if ((GetAsyncKeyState('F') & 0x8000) != 0) pState->Gamepad.sThumbLX = -32768;
	// if ((GetAsyncKeyState('H') & 0x8000) != 0) pState->Gamepad.sThumbLX = 32767;
	//
	// if ((GetAsyncKeyState(VK_NUMPAD8) & 0x8000) != 0) pState->Gamepad.sThumbRY = 32767;
	// if ((GetAsyncKeyState(VK_NUMPAD2) & 0x8000) != 0) pState->Gamepad.sThumbRY = -32768;
	// if ((GetAsyncKeyState(VK_NUMPAD4) & 0x8000) != 0) pState->Gamepad.sThumbRX = -32768;
	// if ((GetAsyncKeyState(VK_NUMPAD6) & 0x8000) != 0) pState->Gamepad.sThumbRX = 32767;
	
	// read state from static variable:
	*pState = state;
	
	pState->dwPacketNumber = GetTickCount();

	// if (dwUserIndex == 0)
		return ERROR_SUCCESS;
	// else
	// 	return ERROR_DEVICE_NOT_CONNECTED;
}

DLLEXPORT DWORD WINAPI XInputSetState(_In_ DWORD dwUserIndex, _In_ XINPUT_VIBRATION *pVibration)
{
	//pVibration->wLeftMotorSpeed
	//pVibration->wRightMotorSpeed

	if (dwUserIndex == 0)
		return ERROR_SUCCESS;
	else
		return ERROR_DEVICE_NOT_CONNECTED;
}


DLLEXPORT DWORD WINAPI XInputGetCapabilities(_In_ DWORD dwUserIndex, _In_ DWORD dwFlags, _Out_ XINPUT_CAPABILITIES *pCapabilities)
{
	// Set some defaults for the virtual device
	pCapabilities->Type = 1;
	pCapabilities->SubType = 1; //customizable subtype
	pCapabilities->Flags = 0; // we do not support sound
	pCapabilities->Vibration.wLeftMotorSpeed = pCapabilities->Vibration.wRightMotorSpeed = 0xFF;
	pCapabilities->Gamepad.bLeftTrigger = pCapabilities->Gamepad.bRightTrigger = 0xFF;

	pCapabilities->Gamepad.sThumbLX = (SHORT)-64;
	pCapabilities->Gamepad.sThumbLY = (SHORT)-64;
	pCapabilities->Gamepad.sThumbRX = (SHORT)-64;
	pCapabilities->Gamepad.sThumbRY = (SHORT)-64;
	pCapabilities->Gamepad.wButtons = (WORD)0xF3FF;

	// Done
	return ERROR_SUCCESS;
}

DLLEXPORT void WINAPI XInputEnable(_In_ BOOL enable)
{
	
}

DLLEXPORT DWORD WINAPI XInputGetDSoundAudioDeviceGuids(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
	if (dwUserIndex == 0)
		return ERROR_SUCCESS;
	else
		return ERROR_DEVICE_NOT_CONNECTED;
}

DLLEXPORT DWORD WINAPI XInputGetBatteryInformation(_In_ DWORD dwUserIndex, _In_ BYTE devType, _Out_ XINPUT_BATTERY_INFORMATION *pBatteryInformation)
{
	if (dwUserIndex == 0)
		return ERROR_SUCCESS;
	else
		return ERROR_DEVICE_NOT_CONNECTED;
}

DLLEXPORT DWORD WINAPI XInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke)
{
	if (dwUserIndex == 0)
		return ERROR_SUCCESS;
	else
		return ERROR_DEVICE_NOT_CONNECTED;
}

DLLEXPORT DWORD WINAPI XInputGetStateEx(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE *pState)
{
	if (dwUserIndex == 0)
		return ERROR_SUCCESS;
	else
		return ERROR_DEVICE_NOT_CONNECTED;
}

DLLEXPORT DWORD WINAPI XInputWaitForGuideButton(_In_ DWORD dwUserIndex, _In_ DWORD dwFlag, _In_ LPVOID pVoid)
{
	if (dwUserIndex == 0)
		return ERROR_SUCCESS;
	else
		return ERROR_DEVICE_NOT_CONNECTED;
}

DLLEXPORT DWORD XInputCancelGuideButtonWait(_In_ DWORD dwUserIndex)
{
	if (dwUserIndex == 0)
		return ERROR_SUCCESS;
	else
		return ERROR_DEVICE_NOT_CONNECTED;
}

DLLEXPORT DWORD XInputPowerOffController(_In_ DWORD dwUserIndex)
{
	if (dwUserIndex == 0)
		return ERROR_SUCCESS;
	else
		return ERROR_DEVICE_NOT_CONNECTED;
}

// Override button state
DLLEXPORT void XInputSetButtonState(_In_ WORD wButton, _In_ BOOL bPressed)
{
	if (bPressed)
		state.Gamepad.wButtons |= wButton;
	else
		state.Gamepad.wButtons &= ~wButton;
}

// Override trigger state
DLLEXPORT void XInputSetTriggerState(_In_ BOOL bLeft, _In_ BYTE bValue)
{
	if (bLeft)
		state.Gamepad.bLeftTrigger = bValue;
	else
		state.Gamepad.bRightTrigger = bValue;
}

// Override thumb state
DLLEXPORT void XInputSetThumbState(_In_ BOOL bLeft, _In_ SHORT sX, _In_ SHORT sY)
{
	if (bLeft)
	{
		state.Gamepad.sThumbLX = sX;
		state.Gamepad.sThumbLY = sY;
	}
	else
	{
		state.Gamepad.sThumbRX = sX;
		state.Gamepad.sThumbRY = sY;
	}
}