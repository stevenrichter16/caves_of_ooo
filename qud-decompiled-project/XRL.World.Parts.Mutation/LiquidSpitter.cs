using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleLib.Console;

namespace XRL.World.Parts.Mutation;

[Serializable]
public class LiquidSpitter : BaseMutation
{
	public static readonly Dictionary<string, string> LiquidAnimationColors = new Dictionary<string, string>
	{
		{ "lava", "&W" },
		{ "acid", "&G" },
		{ "honey", "&w" },
		{ "slime", "&g" },
		{ "water", "&B" },
		{ "salt", "&Y" },
		{ "base", "&B" },
		{ "wine", "&m" },
		{ "asphalt", "&K" },
		{ "oil", "&K" },
		{ "blood", "&r" },
		{ "sludge", "&w" },
		{ "goo", "&G" },
		{ "putrid", "&g" },
		{ "gel", "&Y" },
		{ "ooze", "&K" },
		{ "cider", "&w" },
		{ "convalessence", "&C" },
		{ "neutronflux", "&y" },
		{ "cloning", "&Y" },
		{ "proteangunk", "&c" },
		{ "wax", "&Y" },
		{ "ink", "&K" },
		{ "sap", "&W" },
		{ "brainbrine", "&g" },
		{ "algae", "&g" },
		{ "sunslag", "&Y" },
		{ "warmstatic", "&y" }
	};

	public static readonly string[] Exclude = new string[1] { "neutronflux" };

	public static readonly string[] Dilute = new string[4] { "cloning", "brainbrine", "sunslag", "warmstatic" };

	public const string ABL_CMD = "CommandSpitLiquid";

	public List<string> Liquids = new List<string>();

	private string _LiquidName;

	public string LiquidName
	{
		get
		{
			if (_LiquidName == null)
			{
				LiquidVolume liquidVolume = CreatePool().LiquidVolume;
				_LiquidName = liquidVolume.GetLiquidName();
			}
			return _LiquidName;
		}
	}

	public string GetAnimationColor
	{
		get
		{
			if (Liquids.Count > 0 && LiquidAnimationColors.TryGetValue(Liquids.GetRandomElementCosmetic(), out var value))
			{
				return value;
			}
			return "&y";
		}
	}

	public LiquidSpitter()
	{
		base.Type = "Physical";
	}

	public LiquidSpitter(string Liquid)
		: this()
	{
		AddLiquid(Liquid);
	}

	public override bool CanLevel()
	{
		return false;
	}

	public int GetCooldown()
	{
		return 40;
	}

	public override IPart DeepCopy(GameObject Parent)
	{
		LiquidSpitter obj = base.DeepCopy(Parent) as LiquidSpitter;
		obj.Liquids = new List<string>(Liquids);
		return obj;
	}

	public override void CollectStats(Templates.StatCollector stats, int Level)
	{
		stats.Set("Liquid", ColorUtility.StripBackgroundFormatting(LiquidName));
		stats.Set("Range", 8);
		stats.Set("Area", "3x3");
		stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), GetCooldown());
	}

	public override bool WantEvent(int ID, int cascade)
	{
		if (!base.WantEvent(ID, cascade) && ID != AIGetOffensiveAbilityListEvent.ID && ID != PooledEvent<CommandEvent>.ID)
		{
			return ID == PooledEvent<GetItemElementsEvent>.ID;
		}
		return true;
	}

	public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
	{
		if (E.Distance <= 8 && IsMyActivatedAbilityAIUsable(ActivatedAbilityID) && GameObject.Validate(E.Target) && E.Actor.HasLOSTo(E.Target, IncludeSolid: true, BlackoutStops: false, UseTargetability: true))
		{
			E.Add("CommandSpitLiquid");
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(CommandEvent E)
	{
		if (E.Command != "CommandSpitLiquid")
		{
			return base.HandleEvent(E);
		}
		if (Liquids.Count == 0)
		{
			MetricsManager.LogError($"No liquids to spit (Creature: {ParentObject})");
			return ParentObject.ShowFailure("You lack a liquid to spit!");
		}
		List<Cell> list = PickBurst(1, 8, Locked: false, AllowVis.OnlyVisible, "Spit");
		if (list.IsNullOrEmpty())
		{
			return false;
		}
		if (list.Any((Cell C) => C.DistanceTo(ParentObject) > 9))
		{
			return ParentObject.ShowFailure("That is out of range! (8 squares)");
		}
		ParentObject.PlayWorldSound("Sounds/Abilities/sfx_ability_creature_liquid_spit");
		SlimeGlands.SlimeAnimation(GetAnimationColor, ParentObject.CurrentCell, list[0]);
		UseEnergy(1000, "Physical Mutation Spit Liquid");
		CooldownMyActivatedAbility(ActivatedAbilityID, 40);
		DidX("spit", "a puddle of " + LiquidName, "!", null, null, ParentObject);
		int num = 0;
		for (int count = list.Count; num < count; num++)
		{
			if (80.in100() || num == 0)
			{
				list[num].AddObject(CreatePool());
			}
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(GetItemElementsEvent E)
	{
		if (E.IsRelevantCreature(ParentObject))
		{
			if (Liquids.Contains("salt"))
			{
				E.Add("salt", 1);
			}
			else if (Liquids.Contains("convalessence"))
			{
				E.Add("ice", 1);
			}
			else if (Liquids.Contains("sunslag"))
			{
				E.Add("stars", 1);
			}
			else if (Liquids.Contains("brainbrine"))
			{
				E.Add("scholarship", 1);
			}
		}
		return base.HandleEvent(E);
	}

	public override bool AllowStaticRegistration()
	{
		return true;
	}

	public override string GetDescription()
	{
		return "You spit a puddle of " + ColorUtility.StripBackgroundFormatting(LiquidName) + ".";
	}

	public override string GetLevelText(int Level)
	{
		return "Range: 8\nArea: 3x3\nCooldown: 10 rounds";
	}

	public override bool Mutate(GameObject GO, int Level)
	{
		ActivatedAbilityID = AddMyActivatedAbility("Spit Liquid", "CommandSpitLiquid", "Physical Mutations", null, "\u00ad");
		return base.Mutate(GO, Level);
	}

	public override bool Unmutate(GameObject GO)
	{
		RemoveMyActivatedAbility(ref ActivatedAbilityID);
		return base.Unmutate(GO);
	}

	public GameObject CreatePool()
	{
		GameObject gameObject = GameObjectFactory.Factory.CreateObject("BasePool");
		LiquidVolume liquidVolume = gameObject.LiquidVolume;
		liquidVolume.ComponentLiquids.Clear();
		foreach (string liquid in Liquids)
		{
			liquidVolume.ComponentLiquids[liquid] = liquidVolume.StartVolume.RollCached();
		}
		liquidVolume.Update();
		if (liquidVolume.Primary.IsNullOrEmpty())
		{
			MetricsManager.LogError("Spitting liquid without primary " + string.Format("(Liquids: {0}, Creature: {1}, Volume: {2}, ", string.Join(", ", Liquids), ParentObject, liquidVolume.Volume) + "Components: " + string.Join(", ", liquidVolume.ComponentLiquids) + ")");
		}
		return gameObject;
	}

	public void AddLiquid(string ID)
	{
		if (!Liquids.Contains(ID) && !Exclude.Contains(ID))
		{
			Liquids.Add(ID);
			_LiquidName = null;
			if (Liquids.Count == 1 && Dilute.Contains(ID))
			{
				Liquids.Add("water");
			}
		}
	}
}
