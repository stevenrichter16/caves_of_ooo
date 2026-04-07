using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;

public class FreeholdCustomDualSenseEdgeGamepadHID : Gamepad
{
	static FreeholdCustomDualSenseEdgeGamepadHID()
	{
		InputSystem.RegisterLayout<DualSenseGamepadHID>("DualSense Edge (Freehold Games)", default(InputDeviceMatcher).WithInterface("HID").WithManufacturer("Sony Interactive Entertainment").WithProduct("DualSense Edge Wireless Controller"));
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
	}
}
