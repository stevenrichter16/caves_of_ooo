using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace XRL.UI;

public class CommandBinding : IDisposable
{
	public enum EVALUATE_MODE
	{
		IS_PRESSED,
		WAS_PERFORMED_THIS_FRAME,
		WAS_PRESSED_THIS_FRAME,
		WAS_RELEASED_THIS_FRAME
	}

	public bool enabled;

	public bool isDefault = true;

	public GameCommand command;

	public InputAction _inputAction;

	public Dictionary<int, List<InputAction>> keyboardSubActions = new Dictionary<int, List<InputAction>>();

	public Dictionary<int, List<InputAction>> mouseSubActions = new Dictionary<int, List<InputAction>>();

	public Dictionary<int, List<InputAction>> gamepadSubActions = new Dictionary<int, List<InputAction>>();

	public bool UseSubactions = true;

	public string name => _inputAction?.name ?? "(null)";

	public bool SharesBindingsWith(CommandBinding with)
	{
		if (command != null && with.command != null)
		{
			return command.SharesBindsWith(with.command);
		}
		return true;
	}

	public void Enable()
	{
		enabled = true;
		if (!_inputAction.enabled)
		{
			_inputAction.Enable();
		}
	}

	public void Disable()
	{
		enabled = false;
	}

	public bool IsMapped()
	{
		InputAction inputAction = _inputAction;
		if (inputAction == null)
		{
			return false;
		}
		return inputAction.bindings.Count > 0;
	}

	public IEnumerable<int> GetConsoleKeycodes()
	{
		foreach (int item in _inputAction?.GetConsoleKeycodes())
		{
			yield return item;
		}
	}

	public T ReadValue<T>() where T : struct
	{
		return _inputAction.ReadValue<T>();
	}

	public void InitSubactions()
	{
		if (_inputAction == null || _inputAction.bindings.Count <= 0)
		{
			return;
		}
		int num = 0;
		string text = null;
		int num2 = 0;
		for (int i = 0; i < _inputAction.bindings.Count; i++)
		{
			InputBinding inputBinding = _inputAction.bindings[i];
			if (inputBinding.isComposite)
			{
				if (inputBinding.path == "OneModifier")
				{
					num2 = 2;
				}
				else if (inputBinding.path == "TwoModifiers")
				{
					num2 = 3;
				}
				else
				{
					MetricsManager.LogError("Unknown composiite " + inputBinding.path);
				}
			}
			else if (inputBinding.isPartOfComposite)
			{
				CommandBindingManager.GetGamepadAltBindings();
				if (CommandBindingManager.keyboardModifierFlags.ContainsKey(inputBinding.path))
				{
					num = (int)(num + CommandBindingManager.keyboardModifierFlags[inputBinding.path]);
				}
				else if (CommandBindingManager.GetGamepadAltBindings() == inputBinding.path)
				{
					num += 8;
				}
				else
				{
					text = inputBinding.path;
				}
				num2--;
			}
			else
			{
				text = inputBinding.path;
			}
			if (inputBinding.isComposite || num2 != 0)
			{
				continue;
			}
			InputAction inputAction = new InputAction(_inputAction.name + "_subaction_" + num + "|" + text, _inputAction.type);
			inputAction.AddBinding(text);
			if (inputBinding.path.StartsWith("<Keyboard>"))
			{
				if (!keyboardSubActions.ContainsKey(num))
				{
					keyboardSubActions.Add(num, new List<InputAction>());
				}
				keyboardSubActions[num].Add(inputAction);
			}
			else if (inputBinding.path.StartsWith("<Gamepad>"))
			{
				if (!gamepadSubActions.ContainsKey(num))
				{
					gamepadSubActions.Add(num, new List<InputAction>());
				}
				gamepadSubActions[num].Add(inputAction);
			}
			else if (inputBinding.path.StartsWith("<Mouse>"))
			{
				if (!mouseSubActions.ContainsKey(num))
				{
					mouseSubActions.Add(num, new List<InputAction>());
				}
				mouseSubActions[num].Add(inputAction);
			}
			else
			{
				MetricsManager.LogError("Unknown binding path while generating subactions " + inputBinding.path);
			}
			inputAction.Enable();
			num = 0;
		}
	}

	public bool Evaluate(EVALUATE_MODE mode)
	{
		if (UseSubactions)
		{
			if (keyboardSubActions != null && keyboardSubActions.TryGetValue(CommandBindingManager.GetFrameKeyboardModifier(), out var value))
			{
				for (int i = 0; i < value.Count; i++)
				{
					if (mode == EVALUATE_MODE.IS_PRESSED && value[i].IsPressed())
					{
						return true;
					}
					if (mode == EVALUATE_MODE.WAS_PERFORMED_THIS_FRAME && value[i].WasPerformedThisFrame())
					{
						return true;
					}
					if (mode == EVALUATE_MODE.WAS_PRESSED_THIS_FRAME && value[i].WasPressedThisFrame())
					{
						return true;
					}
					if (mode == EVALUATE_MODE.WAS_RELEASED_THIS_FRAME && value[i].WasReleasedThisFrame())
					{
						return true;
					}
				}
			}
			if (mouseSubActions != null && mouseSubActions.TryGetValue(CommandBindingManager.GetFrameKeyboardModifier(), out value))
			{
				for (int j = 0; j < value.Count; j++)
				{
					if (mode == EVALUATE_MODE.IS_PRESSED && value[j].IsPressed())
					{
						return true;
					}
					if (mode == EVALUATE_MODE.WAS_PERFORMED_THIS_FRAME && value[j].WasPerformedThisFrame())
					{
						return true;
					}
					if (mode == EVALUATE_MODE.WAS_PRESSED_THIS_FRAME && value[j].WasPressedThisFrame())
					{
						return true;
					}
					if (mode == EVALUATE_MODE.WAS_RELEASED_THIS_FRAME && value[j].WasReleasedThisFrame())
					{
						return true;
					}
				}
			}
			if (gamepadSubActions != null && gamepadSubActions.TryGetValue(CommandBindingManager.GetFrameGamepadModifier(), out value))
			{
				for (int k = 0; k < value.Count; k++)
				{
					if (mode == EVALUATE_MODE.IS_PRESSED && value[k].IsPressed())
					{
						return true;
					}
					if (mode == EVALUATE_MODE.WAS_PERFORMED_THIS_FRAME && value[k].WasPerformedThisFrame())
					{
						return true;
					}
					if (mode == EVALUATE_MODE.WAS_PRESSED_THIS_FRAME && value[k].WasPressedThisFrame())
					{
						return true;
					}
					if (mode == EVALUATE_MODE.WAS_RELEASED_THIS_FRAME && value[k].WasReleasedThisFrame())
					{
						return true;
					}
				}
			}
			return false;
		}
		return mode switch
		{
			EVALUATE_MODE.IS_PRESSED => _inputAction.IsPressed(), 
			EVALUATE_MODE.WAS_PERFORMED_THIS_FRAME => _inputAction.WasPerformedThisFrame(), 
			EVALUATE_MODE.WAS_PRESSED_THIS_FRAME => _inputAction.WasPressedThisFrame(), 
			EVALUATE_MODE.WAS_RELEASED_THIS_FRAME => _inputAction.WasReleasedThisFrame(), 
			_ => false, 
		};
	}

	private bool _IsPressed(InputAction a)
	{
		return a.IsPressed();
	}

	public bool IsPressed(bool forceEnable = false)
	{
		if (!forceEnable && !enabled)
		{
			return false;
		}
		return Evaluate(EVALUATE_MODE.IS_PRESSED);
	}

	private bool _WasPerformedThisFrame(InputAction a)
	{
		return a.WasPerformedThisFrame();
	}

	public bool WasPerformedThisFrame(bool ignoreEnabled = false)
	{
		if (!enabled && !ignoreEnabled)
		{
			return false;
		}
		return Evaluate(EVALUATE_MODE.WAS_PERFORMED_THIS_FRAME);
	}

	public bool WasReleasedThisFrame(bool ignoreEnabled = false)
	{
		if (!enabled && !ignoreEnabled)
		{
			return false;
		}
		return Evaluate(EVALUATE_MODE.WAS_RELEASED_THIS_FRAME);
	}

	private bool _WasPressedThisFrame(InputAction a)
	{
		return a.WasPressedThisFrame();
	}

	public bool WasPressedThisFrame(bool ignoreEnabled = false)
	{
		if (!enabled && !ignoreEnabled)
		{
			return false;
		}
		return Evaluate(EVALUATE_MODE.WAS_PRESSED_THIS_FRAME);
	}

	public List<string> SerializedFormat()
	{
		if (isDefault)
		{
			return null;
		}
		return _inputAction.SerializedFormat();
	}

	public void Dispose()
	{
		if (keyboardSubActions != null)
		{
			foreach (KeyValuePair<int, List<InputAction>> keyboardSubAction in keyboardSubActions)
			{
				foreach (InputAction item in keyboardSubAction.Value)
				{
					item.Dispose();
				}
			}
		}
		if (mouseSubActions != null)
		{
			foreach (KeyValuePair<int, List<InputAction>> mouseSubAction in mouseSubActions)
			{
				foreach (InputAction item2 in mouseSubAction.Value)
				{
					item2.Dispose();
				}
			}
		}
		if (gamepadSubActions != null)
		{
			foreach (KeyValuePair<int, List<InputAction>> gamepadSubAction in gamepadSubActions)
			{
				foreach (InputAction item3 in gamepadSubAction.Value)
				{
					item3.Dispose();
				}
			}
		}
		mouseSubActions.Clear();
		keyboardSubActions.Clear();
		gamepadSubActions.Clear();
		_inputAction?.Dispose();
		_inputAction = null;
	}

	public static CommandBinding FromSerializedFormat(InputAction action, GameCommand cmd, List<string> bindings, bool AllowLegacyUpgrade, string targetSet)
	{
		CommandBinding commandBinding = new CommandBinding();
		commandBinding.command = cmd;
		commandBinding._inputAction = action;
		if (bindings != null)
		{
			commandBinding.isDefault = false;
			int num = 0;
			while (num < bindings.Count)
			{
				if (bindings[num] == InputSystemExtensions.COMPOSITE)
				{
					InputActionSetupExtensions.CompositeSyntax compositeSyntax = action.AddCompositeBinding(bindings[num + 1]);
					if (bindings[num + 1] == "OneModifier")
					{
						compositeSyntax.With("Binding", bindings[num + 2]);
						compositeSyntax.With("Modifier", resolveAlt(bindings[num + 3]));
						num += 4;
					}
					else if (bindings[num + 1] == "TwoModifiers")
					{
						compositeSyntax.With("Binding", bindings[num + 2]);
						compositeSyntax.With("Modifier1", resolveAlt(bindings[num + 3]));
						compositeSyntax.With("Modifier2", resolveAlt(bindings[num + 4]));
						num += 5;
					}
					else
					{
						MetricsManager.LogError("Unknown composite type " + bindings[num + 1] + " - aborting load for this action");
						num++;
					}
				}
				else
				{
					action.AddBinding(bindings[num++]);
				}
			}
		}
		else
		{
			KeyMap keyMap = (AllowLegacyUpgrade ? CommandBindingManager.GetLegacyKeymap() : null);
			if (cmd.keyboardBindings.Count > 0 || AllowLegacyUpgrade)
			{
				bool flag = false;
				try
				{
					try
					{
						if (keyMap != null && !cmd.SkipUpgrade)
						{
							List<int> list = new List<int>();
							try
							{
								list.Add(keyMap.PrimaryMapCommandToKeyLayer.Where((KeyValuePair<string, Dictionary<string, int>> l) => l.Value.Any((KeyValuePair<string, int> legacyCmd) => legacyCmd.Key.ToLower() == cmd.UpgradeFrom.ToLower())).FirstOrDefault().Value.Where((KeyValuePair<string, int> v) => v.Key.ToLower() == cmd.ID.ToLower())?.FirstOrDefault().Value ?? 0);
							}
							catch
							{
							}
							try
							{
								IEnumerable<KeyValuePair<string, int>> source = keyMap.SecondaryMapCommandToKeyLayer.Where((KeyValuePair<string, Dictionary<string, int>> l) => l.Value.Any((KeyValuePair<string, int> legacyCmd) => legacyCmd.Key.ToLower() == cmd.UpgradeFrom.ToLower())).FirstOrDefault().Value.Where((KeyValuePair<string, int> v) => v.Key.ToLower() == cmd.ID.ToLower());
								list.Add(source.FirstOrDefault().Value);
							}
							catch
							{
							}
							foreach (int item in list)
							{
								if (CommandBindingManager.AddKeysValueToActionAsBinding(item, action))
								{
									flag = true;
									commandBinding.isDefault = false;
								}
							}
						}
					}
					catch (Exception ex)
					{
						MetricsManager.LogEditorError(ex.ToString());
					}
				}
				catch (Exception x)
				{
					MetricsManager.LogException("KeyMapping upgrade old keymap", x);
				}
				if (!flag)
				{
					foreach (GameCommand.KeyboardBinding keyboardBinding in cmd.keyboardBindings)
					{
						if (!string.IsNullOrEmpty(keyboardBinding.Set) && !(keyboardBinding.Set == targetSet) && (!(keyboardBinding.Set == "default") || !string.IsNullOrEmpty(targetSet)))
						{
							continue;
						}
						List<string> list2 = keyboardBinding.Modifier?.CachedCommaExpansion();
						if (list2 == null || list2.Count == 0)
						{
							action.AddBinding("<Keyboard>/" + keyboardBinding.Key);
							continue;
						}
						if (list2.Count == 1)
						{
							action.AddCompositeBinding("OneModifier").With("Binding", "<Keyboard>/" + keyboardBinding.Key).With("Modifier", "<Keyboard>/" + list2[0]);
							continue;
						}
						if (list2.Count == 2)
						{
							action.AddCompositeBinding("TwoModifiers").With("Binding", "<Keyboard>/" + keyboardBinding.Key).With("Modifier1", "<Keyboard>/" + list2[0])
								.With("Modifier2", "<Keyboard>/" + list2[1]);
							continue;
						}
						throw new Exception("Invalid or too many modifiers on " + cmd.ID + "!");
					}
				}
			}
			foreach (GameCommand.MouseBinding mouseBinding in cmd.mouseBindings)
			{
				if (!string.IsNullOrEmpty(mouseBinding.Set) && !(mouseBinding.Set == targetSet) && (!(mouseBinding.Set == "default") || !string.IsNullOrEmpty(targetSet)))
				{
					continue;
				}
				List<string> list3 = mouseBinding.Modifier?.CachedCommaExpansion();
				if (list3 == null || list3.Count == 0)
				{
					action.AddBinding("<Mouse>/" + mouseBinding.Button);
					continue;
				}
				if (list3.Count == 1)
				{
					action.AddCompositeBinding("OneModifier").With("Binding", "<Mouse>/" + mouseBinding.Button).With("Modifier", "<Keyboard>/" + list3[0]);
					continue;
				}
				if (list3.Count == 2)
				{
					action.AddCompositeBinding("TwoModifiers").With("Binding", "<Mouse>/" + mouseBinding.Button).With("Modifier1", "<Keyboard>/" + list3[0])
						.With("Modifier2", "<Keyboard>/" + list3[1]);
					continue;
				}
				throw new Exception("Invalid or too many modifiers on " + cmd.ID + "!");
			}
			if (cmd.gamepadBindings.Count > 0)
			{
				foreach (GameCommand.GamepadBinding gamepadBinding in cmd.gamepadBindings)
				{
					if (!gamepadBinding.Alt)
					{
						action.AddBinding("<Gamepad>/" + gamepadBinding.Button);
					}
					else
					{
						action.AddCompositeBinding("OneModifier").With("Binding", "<Gamepad>/" + gamepadBinding.Button).With("Modifier", CommandBindingManager.CurrentMap.ResolveGamepadAltBind());
					}
				}
			}
		}
		if (action.name == "GamepadAlt")
		{
			commandBinding.UseSubactions = false;
			CommandBindingManager.gamepadAltAction = commandBinding._inputAction;
		}
		else
		{
			commandBinding.InitSubactions();
		}
		return commandBinding;
		static string resolveAlt(string binding)
		{
			if (binding == InputSystemExtensions.GAMEPADALT || binding.StartsWith("<Gamepad>"))
			{
				return CommandBindingManager.CurrentMap.ResolveGamepadAltBind();
			}
			return binding;
		}
	}

	public static CommandBinding FromInputAction(InputAction action)
	{
		CommandBinding commandBinding = new CommandBinding();
		commandBinding._inputAction = action;
		if (action.name == "GamepadAlt")
		{
			commandBinding.UseSubactions = false;
			CommandBindingManager.gamepadAltAction = commandBinding._inputAction;
		}
		else
		{
			commandBinding.InitSubactions();
		}
		return commandBinding;
	}
}
