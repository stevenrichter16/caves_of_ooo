using System.Collections.Generic;
using System.Linq;
using XRL.Messages;
using XRL.UI;
using XRL.Wish;
using XRL.World.Parts;

namespace XRL.World.Capabilities;

[HasWishCommand]
public static class AutoAct
{
	public const string OngoingActionSetting = "o";

	public static string ResumeSetting = "";

	public static int Digging = 0;

	public static OngoingAction _Action = null;

	public static OngoingAction _ResumeAction = null;

	public static bool Attacking;

	private static List<string> PropsToRemove = new List<string>();

	public static string Setting
	{
		get
		{
			return The.Core.PlayerWalking;
		}
		set
		{
			The.Core.PlayerWalking = value;
			Digging = 0;
		}
	}

	public static OngoingAction Action
	{
		get
		{
			return _Action;
		}
		set
		{
			if (value != null && IsActive())
			{
				ResumeAction = _Action;
				ResumeSetting = Setting;
			}
			_Action = value;
			if (_Action != null)
			{
				Setting = "o";
			}
		}
	}

	public static OngoingAction ResumeAction
	{
		get
		{
			return _ResumeAction;
		}
		set
		{
			_ResumeAction = value;
			if (_ResumeAction != null)
			{
				ResumeSetting = "o";
			}
		}
	}

	public static bool IsActive(string What)
	{
		if (What != null && What != "")
		{
			return What != "ReopenMissileUI";
		}
		return false;
	}

	public static bool IsActive(bool IgnoreAutoget = false)
	{
		if (IgnoreAutoget && Setting == "g")
		{
			return false;
		}
		return IsActive(Setting);
	}

	public static bool IsInterruptable(string What)
	{
		return IsActive(What);
	}

	public static bool IsInterruptable()
	{
		return IsInterruptable(Setting);
	}

	public static bool ShouldHostilesInterrupt(string What, OngoingAction Action = null, bool logSpot = false, bool popSpot = false, bool CheckingPrior = true)
	{
		if (!IsActive(What))
		{
			return false;
		}
		if (What == "g")
		{
			if (Options.AutogetIfHostiles)
			{
				return false;
			}
		}
		else if (What == "o" && Action != null && !Action.ShouldHostilesInterrupt())
		{
			return false;
		}
		return The.Player.ArePerceptibleHostilesNearby(logSpot, popSpot, null, Action, What, CheckingPrior: CheckingPrior, IgnoreEasierThan: IsResting(What) ? int.MinValue : Options.AutoexploreIgnoreEasyEnemies, IgnoreFartherThan: Options.AutoexploreIgnoreDistantEnemies, IgnorePlayerTarget: IsCombat(What));
	}

	public static bool ShouldHostilesInterrupt(bool logSpot = false, bool popSpot = false, bool CheckingPrior = true)
	{
		return ShouldHostilesInterrupt(Setting, Action, logSpot, popSpot, CheckingPrior);
	}

	public static bool CheckHostileInterrupt(bool logSpot)
	{
		if (ShouldHostilesInterrupt(logSpot, popSpot: false, CheckingPrior: false))
		{
			Interrupt();
			return true;
		}
		return false;
	}

	public static bool CheckHostileInterrupt()
	{
		return CheckHostileInterrupt(!IsOnlyGathering() || Sidebar.AnyAutogotItems());
	}

	public static bool ShouldHostilesPreventAutoget()
	{
		return ShouldHostilesInterrupt("g");
	}

	public static bool IsMovement(string What, OngoingAction Action = null)
	{
		if (!IsActive(What))
		{
			return false;
		}
		switch (What[0])
		{
		case '.':
			return false;
		case 'g':
			return false;
		case 'o':
			return Action?.IsMovement() ?? false;
		case 'r':
		case 'z':
			return false;
		default:
			return true;
		}
	}

	public static bool IsMovement()
	{
		return IsMovement(Setting, Action);
	}

	public static bool IsAnyMovement()
	{
		if (!IsMovement(Setting, Action))
		{
			return IsMovement(ResumeSetting, ResumeAction);
		}
		return true;
	}

	public static bool IsCombat(string What, OngoingAction Action = null)
	{
		return What switch
		{
			"ReopenMissileUI" => true, 
			"a" => true, 
			"o" => Action?.IsCombat() ?? false, 
			_ => false, 
		};
	}

	public static bool IsCombat()
	{
		return IsCombat(Setting, Action);
	}

	public static bool IsAnyCombat()
	{
		if (!IsCombat(Setting, Action))
		{
			return IsCombat(ResumeSetting, ResumeAction);
		}
		return true;
	}

	public static bool IsExploration(string What, OngoingAction Action = null)
	{
		if (What == "?")
		{
			return true;
		}
		if (What == "o")
		{
			return Action?.IsExploration() ?? false;
		}
		return false;
	}

	public static bool IsExploration()
	{
		return IsExploration(Setting, Action);
	}

	public static bool IsAnyExploration()
	{
		if (!IsExploration(Setting, Action))
		{
			return IsExploration(ResumeSetting, ResumeAction);
		}
		return true;
	}

	public static bool IsGathering(string What, OngoingAction Action = null)
	{
		if (What == "g")
		{
			return true;
		}
		if (What == "o")
		{
			return Action?.IsGathering() ?? false;
		}
		return false;
	}

	public static bool IsGathering()
	{
		return IsGathering(Setting, Action);
	}

	public static bool IsAnyGathering()
	{
		if (!IsGathering(Setting, Action))
		{
			return IsGathering(ResumeSetting, ResumeAction);
		}
		return true;
	}

	public static bool IsOnlyGathering()
	{
		if (IsGathering(Setting, Action))
		{
			if (!ResumeSetting.IsNullOrEmpty())
			{
				return IsGathering(ResumeSetting, ResumeAction);
			}
			return true;
		}
		return false;
	}

	public static bool IsResting(string What, OngoingAction Action = null)
	{
		if (!IsActive(What))
		{
			return false;
		}
		switch (What[0])
		{
		case '.':
		case 'r':
		case 'z':
			return true;
		case 'o':
			return Action?.IsResting() ?? false;
		default:
			return false;
		}
	}

	public static bool IsResting()
	{
		return IsResting(Setting, Action);
	}

	public static bool IsAnyResting()
	{
		if (!IsResting(Setting, Action))
		{
			return IsResting(ResumeSetting, ResumeAction);
		}
		return true;
	}

	public static bool IsRateLimited(string What, OngoingAction Action = null)
	{
		if (What == "o")
		{
			return Action?.IsRateLimited() ?? false;
		}
		if (IsGathering(What, Action))
		{
			return true;
		}
		if (IsCombat(What, Action))
		{
			return true;
		}
		if (IsMovement(What, Action) && Digging <= 0)
		{
			return true;
		}
		return false;
	}

	public static bool IsRateLimited()
	{
		return IsRateLimited(Setting, Action);
	}

	public static bool IsAnyRateLimited()
	{
		if (!IsRateLimited(Setting, Action))
		{
			return IsRateLimited(ResumeSetting, ResumeAction);
		}
		return true;
	}

	public static string GetDescription(string What, OngoingAction action)
	{
		if (!IsActive(What))
		{
			return "acting";
		}
		switch (What[0])
		{
		case '?':
			return "exploring";
		case '.':
			return "waiting";
		case 'd':
			if (Digging > 0)
			{
				return "digging";
			}
			break;
		case 'g':
			return "gathering";
		case 'o':
			return action?.GetDescription() ?? "acting";
		case 'r':
		case 'z':
			return "resting";
		case 'a':
			return "attacking";
		}
		return "moving";
	}

	public static string GetDescription()
	{
		if (!ResumeSetting.IsNullOrEmpty())
		{
			return GetDescription(ResumeSetting, ResumeAction);
		}
		return GetDescription(Setting, Action);
	}

	public static void ClearAutoMoveStop()
	{
		GameObject.AutomoveInterruptTurn = -2147483648L;
	}

	public static void Interrupt(string Because = null, Cell IndicateCell = null, GameObject IndicateObject = null, bool IsThreat = false)
	{
		GameObject.AutomoveInterruptBecause = Because;
		GameObject.AutomoveInterruptTurn = The.Game.Turns;
		if (IsActive())
		{
			if (Because == null && Action != null)
			{
				Because = Action.GetInterruptBecause();
			}
			if (!Because.IsNullOrEmpty() && (!IsOnlyGathering() || Sidebar.AnyAutogotItems()))
			{
				MessageQueue.AddPlayerMessage(Event.NewStringBuilder().Append("{{").Append(IsThreat ? 'r' : 'y')
					.Append("|You stop ")
					.Append(GetDescription())
					.Append(" because ")
					.Append(Because)
					.Append(".}}")
					.ToString());
			}
			if (IndicateObject != null && (!IsOnlyGathering() || Sidebar.AnyAutogotItems()))
			{
				IndicateObject.Indicate(IsThreat);
			}
			if (IndicateCell != null && (!IsOnlyGathering() || Sidebar.AnyAutogotItems()))
			{
				IndicateCell.Indicate(IsThreat);
			}
		}
		ResumeAction?.Interrupt();
		ResumeAction?.End();
		Action?.Interrupt();
		Action?.End();
		Setting = "";
		Action = null;
		ResumeSetting = "";
		ResumeAction = null;
		Attacking = false;
		The.Core.PlayerAvoid.Clear();
	}

	public static void Interrupt(GameObject BecauseOf, bool ShowIndicator = true, bool IsThreat = false)
	{
		if (BecauseOf != null && IsActive())
		{
			if (!IsOnlyGathering() || Sidebar.AnyAutogotItems())
			{
				MessageQueue.AddPlayerMessage(The.Player.GenerateSpotMessage(BecauseOf), IsThreat ? 'r' : 'y');
			}
			if (ShowIndicator)
			{
				BecauseOf.Indicate(IsThreat);
			}
		}
		ResumeAction?.Interrupt();
		ResumeAction?.End();
		Action?.Interrupt();
		Action?.End();
		Setting = "";
		Action = null;
		ResumeSetting = "";
		ResumeAction = null;
		Attacking = false;
		The.Core.PlayerAvoid.Clear();
	}

	public static void Resume()
	{
		Setting = ResumeSetting;
		Action = ResumeAction;
		ResumeSetting = "";
		ResumeAction = null;
	}

	public static bool TryToMove(GameObject Actor, Cell FromCell, ref GameObject LastDoor, Cell ToCell = null, string Direction = null, bool AllowDigging = true, bool OpenDoors = true, bool Peaceful = true, bool PostMoveHostileCheck = true, bool PostMoveSidebarCheck = true)
	{
		GameObject gameObject = LastDoor;
		LastDoor = null;
		if (FromCell == null)
		{
			Interrupt("your spacetime context is incoherent");
			return false;
		}
		if (ToCell == null && !Direction.IsNullOrEmpty())
		{
			ToCell = FromCell.GetCellFromDirection(Direction);
		}
		if (ToCell == null)
		{
			Interrupt("you can go no further");
			return false;
		}
		if (!ToCell.IsAdjacentTo(FromCell))
		{
			Interrupt("something is wrong with the spacetime connectivity of your environment");
			return false;
		}
		if (Direction.IsNullOrEmpty())
		{
			Direction = FromCell.GetDirectionFromCell(ToCell);
		}
		if (Direction.IsNullOrEmpty() || Direction == "." || Direction == "?")
		{
			Interrupt("the direction you seem to be trying to go makes no sense");
			return false;
		}
		if (OpenDoors)
		{
			int i = 0;
			for (int count = ToCell.Objects.Count; i < count; i++)
			{
				GameObject gameObject2 = ToCell.Objects[i];
				if (!gameObject2.TryGetPart<Door>(out var Part) || Part.Open || !gameObject2.PhaseMatches(Actor))
				{
					continue;
				}
				if (gameObject2 == gameObject)
				{
					Interrupt("you cannot keep " + gameObject2.t() + " open", null, gameObject2);
					return false;
				}
				LastDoor = gameObject2;
				Event obj = Event.New("Open");
				obj.SetParameter("Actor", Actor);
				obj.SetParameter("Opener", Actor);
				obj.SetFlag("UsePopupsForFailures", Actor.IsPlayer());
				obj.SetFlag("FromMove", State: true);
				if (!Part.FireEvent(obj))
				{
					if (!gameObject2.IsCreature)
					{
						Interrupt("you cannot open " + gameObject2.t(), null, gameObject2);
						return false;
					}
					continue;
				}
				goto IL_018a;
			}
		}
		if (AllowDigging && ToCell.IsSolidFor(Actor) && Actor.PathAsBurrower)
		{
			if (Digging > 1000)
			{
				Interrupt("you cannot seem to make any progress", ToCell);
				return false;
			}
			int num = 0;
			int j = 0;
			for (int count2 = ToCell.Objects.Count; j < count2; j++)
			{
				num += ToCell.Objects[j].hitpoints;
			}
			if (!Actor.AttackDirection(Direction))
			{
				Interrupt(null, ToCell, null, IsThreat: true);
				return false;
			}
			int num2 = 0;
			int k = 0;
			for (int count3 = ToCell.Objects.Count; k < count3; k++)
			{
				num2 += ToCell.Objects[k].hitpoints;
			}
			if (num2 < num)
			{
				Digging = 1;
			}
			else
			{
				Digging++;
			}
		}
		else
		{
			string direction = Direction;
			bool peaceful = Peaceful;
			if (!Actor.Move(direction, out var Blocking, Forced: false, System: false, IgnoreGravity: false, NoStack: false, AllowDashing: true, DoConfirmations: true, null, null, NearestAvailable: false, null, null, null, peaceful))
			{
				if (Blocking != null)
				{
					Interrupt(Blocking.does("are") + " in the way", null, Blocking);
				}
				else
				{
					Interrupt(null, ToCell);
				}
				return false;
			}
			if (Actor.CurrentCell == FromCell)
			{
				Interrupt("you tried to move but stayed in the same place", FromCell);
				return false;
			}
			Digging = 0;
		}
		goto IL_0368;
		IL_018a:
		Digging = 0;
		goto IL_0368;
		IL_0368:
		if (PostMoveHostileCheck)
		{
			CheckHostileInterrupt();
		}
		if (PostMoveSidebarCheck && Actor.IsPlayer())
		{
			Cell currentCell = Actor.CurrentCell;
			if (currentCell != null && currentCell != FromCell)
			{
				if (currentCell.X > 42 && Sidebar.State == "right")
				{
					Sidebar.SetSidebarState("left");
				}
				else if (currentCell.X < 38 && Sidebar.State == "left")
				{
					Sidebar.SetSidebarState("right");
				}
			}
		}
		return true;
	}

	public static bool TryToMove(GameObject Actor, Cell FromCell, Cell ToCell = null, string Direction = null, bool AllowDigging = true, bool OpenDoors = true, bool Peaceful = true, bool PostMoveHostileCheck = true, bool PostMoveSidebarCheck = true)
	{
		GameObject LastDoor = null;
		return TryToMove(Actor, FromCell, ref LastDoor, ToCell, Direction, AllowDigging, OpenDoors, Peaceful, PostMoveHostileCheck, PostMoveSidebarCheck);
	}

	public static string AutoexploreActionProperty(string Command)
	{
		return "AutoexploreAction" + (Command.IsNullOrEmpty() ? "_" : Command);
	}

	public static int GetAutoexploreActionProperty(GameObject Object, string Command)
	{
		return Object.GetIntProperty(AutoexploreActionProperty(Command));
	}

	public static void SetAutoexploreActionProperty(GameObject Object, string Command, int Value)
	{
		Object.SetIntProperty(AutoexploreActionProperty(Command), Value);
	}

	public static string AutoexploreSuppressionProperty()
	{
		return "AutoexploreSuppression";
	}

	public static bool AutoexploreSuppressed(GameObject Object)
	{
		return Object.GetIntProperty(AutoexploreSuppressionProperty()) > 0;
	}

	public static void SetAutoexploreSuppression(GameObject Object, bool Flag)
	{
		Object.SetIntProperty(AutoexploreSuppressionProperty(), Flag ? 1 : 0, RemoveIfZero: true);
	}

	public static void ResetZoneAutoexploreAction(string Command)
	{
		Zone parentZone = The.PlayerCell.ParentZone;
		string prop = AutoexploreActionProperty(Command);
		parentZone.ForeachObjectWithTagOrProperty(prop, delegate(GameObject obj)
		{
			SetAutoexploreActionProperty(obj, prop, 0);
		});
		ResetZoneAutoexploreSupression();
	}

	public static void ResetZoneAutoexploreSupression()
	{
		Zone parentZone = The.PlayerCell.ParentZone;
		string prop = AutoexploreSuppressionProperty();
		parentZone.ForeachObjectWithTagOrProperty(prop, delegate(GameObject obj)
		{
			SetAutoexploreActionProperty(obj, prop, 0);
		});
	}

	[WishCommand("resetauto", null)]
	public static bool ResetAutoexploreProperties()
	{
		The.PlayerCell.ParentZone.ForeachObject(delegate(GameObject obj)
		{
			PropsToRemove.Clear();
			foreach (string key in obj.IntProperty.Keys)
			{
				if (key.StartsWith("Autoexplore"))
				{
					PropsToRemove.Add(key);
				}
			}
			if (PropsToRemove.Count > 0)
			{
				string text = PropsToRemove.Aggregate(null, (string a, string b) => (a != null) ? (a + ", " + b) : b);
				MessageQueue.AddPlayerMessage("Resetting " + text + " on " + obj.DebugName);
				obj.IntProperty.RemoveAll(PropsToRemove);
				PropsToRemove.Clear();
			}
		});
		return true;
	}
}
