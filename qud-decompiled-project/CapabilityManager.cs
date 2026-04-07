using UnityEngine;
using XRL.UI;

public class CapabilityManager : MonoBehaviour
{
	public static CapabilityManager instance;

	public static GameManager gameManager => GameManager.Instance;

	public static bool AllowKeyboardHotkeys => true;

	public static void SuggestOnscreenKeyboard()
	{
		if (!(Options.GetOption("OptionFloatingKeyboard") != "Yes") && PlatformManager.GetControlType() == PlatformControlType.Deck)
		{
			PlatformManager.TryShowVirtualKeyboard();
		}
	}

	private void Awake()
	{
	}

	public string GetDefaultOptionOverrideForCapabilities(string Option, string Default)
	{
		return PlatformManager.GetOptionDefault(Option) ?? Default;
	}

	public void Init()
	{
		instance = this;
	}
}
