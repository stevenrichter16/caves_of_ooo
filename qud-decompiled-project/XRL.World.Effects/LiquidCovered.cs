using System;
using System.Collections.Generic;
using System.Text;
using XRL.Liquids;
using XRL.UI;
using XRL.World.Parts;

namespace XRL.World.Effects;

[Serializable]
public class LiquidCovered : Effect
{
	public bool Poured;

	public LiquidVolume Liquid;

	public GameObject PouredBy;

	public bool FromCell;

	public override string RemoveSound => null;

	public LiquidCovered()
	{
		DisplayName = "covered in liquid";
		Duration = 1;
	}

	public LiquidCovered(LiquidVolume From, int Drams, int Duration = 1, bool Poured = false, GameObject PouredBy = null, bool FromCell = false)
		: this()
	{
		Liquid = From.Split(Drams);
		base.Duration = Duration;
		this.Poured = Poured;
		this.PouredBy = PouredBy;
		this.FromCell = FromCell;
	}

	public LiquidCovered(string LiquidSpec, int Drams, int Duration = 1, bool Poured = false, GameObject PouredBy = null, bool FromCell = false)
		: this()
	{
		Liquid = new LiquidVolume(LiquidSpec, Drams);
		base.Duration = Duration;
		this.Poured = Poured;
		this.PouredBy = PouredBy;
		this.FromCell = FromCell;
	}

	public override int GetEffectType()
	{
		int num = 67108896;
		if (Liquid != null && Liquid.ConsiderLiquidDangerousToContact())
		{
			num |= 0x2000000;
		}
		return num;
	}

	public override bool SameAs(Effect e)
	{
		LiquidCovered liquidCovered = e as LiquidCovered;
		if (liquidCovered.Poured != Poured)
		{
			return false;
		}
		if (liquidCovered.FromCell != FromCell)
		{
			return false;
		}
		if (Liquid == null != (liquidCovered.Liquid == null))
		{
			return false;
		}
		if (Liquid != null && !liquidCovered.Liquid.SameAs(Liquid))
		{
			return false;
		}
		if (liquidCovered.PouredBy != PouredBy)
		{
			return false;
		}
		return base.SameAs(e);
	}

	public override bool CanApplyToStack()
	{
		return true;
	}

	public override void Write(GameObject Basis, SerializationWriter Writer)
	{
		if (Liquid == null)
		{
			Liquid = new LiquidVolume();
		}
		IPart.Save(Liquid, Writer);
		Writer.WriteGameObject(PouredBy);
		Writer.Write(Poured);
		Writer.Write(FromCell);
	}

	public override void Read(GameObject Basis, SerializationReader Reader)
	{
		Liquid = IPart.Load(null, Reader) as LiquidVolume;
		PouredBy = Reader.ReadGameObject();
		Poured = Reader.ReadBoolean();
		FromCell = Reader.ReadBoolean();
	}

	public override string GetDescription()
	{
		if (Liquid == null)
		{
			return "covered in liquid";
		}
		return Liquid.SmearedName.Color(Liquid.SmearedColor);
	}

	public override bool SuppressInLookDisplay()
	{
		return true;
	}

	public override string GetDetails()
	{
		StringBuilder stringBuilder = Event.NewStringBuilder();
		stringBuilder.Append("Covered in ");
		if (Liquid != null)
		{
			stringBuilder.Append(Liquid.Volume.Things("dram")).Append(" of ").Append(Liquid.GetLiquidName());
		}
		else
		{
			stringBuilder.Append("liquid");
		}
		stringBuilder.Append('.');
		return stringBuilder.ToString();
	}

	public override bool Apply(GameObject Object)
	{
		if (Liquid == null)
		{
			return false;
		}
		bool flag = false;
		try
		{
			if (Object.HasRegisteredEvent("ApplyLiquidCovered") && !Object.FireEvent(Event.New("ApplyLiquidCovered", "Liquid", Liquid)))
			{
				return false;
			}
			Liquid.SmearOn(Object, PouredBy, FromCell);
			if (Liquid.Volume <= 0)
			{
				return false;
			}
			LiquidCovered effect = Object.GetEffect<LiquidCovered>();
			if (effect != null && effect.Liquid != null)
			{
				flag = true;
				effect.Liquid.MixWith(Liquid);
				effect.CheckCapacity();
				if (!FromCell && effect.FromCell)
				{
					effect.FromCell = false;
				}
				return false;
			}
			Liquid.Update();
			flag = true;
			if (Object.HasRegisteredEvent("AppliedLiquidCovered"))
			{
				Object.FireEvent(Event.New("AppliedLiquidCovered", "Liquid", Liquid));
			}
			if (Liquid.ParentObject != null)
			{
				LogError("liquid " + Liquid.GetLiquidDebugDesignation() + " for covering on " + Object.DebugName + " has parent " + Liquid.ParentObject.DebugName);
			}
			return true;
		}
		finally
		{
			if (!flag && Liquid.Volume > 0)
			{
				Liquid.FlowIntoCell(-1, Object.GetCurrentCell());
			}
		}
	}

	public void CheckCapacity()
	{
		if (Liquid == null)
		{
			return;
		}
		int adsorbableDrams = Liquid.GetAdsorbableDrams(base.Object);
		if (Liquid.Volume > adsorbableDrams)
		{
			int num = Liquid.Volume - adsorbableDrams;
			LiquidVolume liquid = Liquid;
			Cell targetCell = base.Object.GetCurrentCell();
			if (!liquid.FlowIntoCell(num, targetCell))
			{
				Liquid.UseDrams(num);
			}
		}
	}

	public void ProcessDynamics()
	{
		if (Liquid == null || Liquid.Volume <= 0)
		{
			base.Object.RemoveEffect(this);
		}
		else
		{
			if (base.Object == null)
			{
				return;
			}
			Cell cell = base.Object.GetCurrentCell();
			if (cell == null)
			{
				return;
			}
			if (FromCell)
			{
				GameObject openLiquidVolume = cell.GetOpenLiquidVolume();
				if (openLiquidVolume != null && Liquid.LiquidSameAs(openLiquidVolume.LiquidVolume))
				{
					return;
				}
				FromCell = false;
			}
			bool num = base.Object.HasTag("Creature");
			int liquidFluidity = Liquid.GetLiquidFluidity();
			int num2 = ((cell == null || !cell.HasWadingDepthLiquid()) ? Liquid.GetLiquidEvaporativity() : 0);
			int num3 = ((!num || !base.Object.IsOrganic) ? Liquid.GetLiquidStaining() : 0);
			int num4 = (base.Object.HasEffect<LiquidStained>() ? Liquid.GetLiquidCleansing() : 0);
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			if (cell == null || (cell.OnWorldMap() && string.IsNullOrEmpty(The.Game?.GetStringGameState("LastLocationOnSurface"))))
			{
				int num9 = liquidFluidity + num2 + num3 + num4;
				if (num9 > 0)
				{
					num5 = Liquid.Volume * liquidFluidity / num9;
					num6 = Liquid.Volume * num2 / num9;
					num7 = Liquid.Volume * num3 / num9;
					num8 = Liquid.Volume * num4 / num9;
					int num10 = num6 + num5 + num7 + num8;
					int num11 = Liquid.Volume - num10;
					if (num11 > 0)
					{
						BallBag<int> ballBag = new BallBag<int>();
						if (liquidFluidity > 0)
						{
							ballBag.Add(1, liquidFluidity);
						}
						if (num2 > 0)
						{
							ballBag.Add(2, num2);
						}
						if (num3 > 0)
						{
							ballBag.Add(3, num3);
						}
						if (num4 > 0)
						{
							ballBag.Add(4, num4);
						}
						for (int i = 0; i < num11; i++)
						{
							switch (ballBag.PeekOne())
							{
							case 1:
								num5++;
								break;
							case 2:
								num6++;
								break;
							case 3:
								num7++;
								break;
							case 4:
								num8++;
								break;
							}
						}
					}
				}
			}
			else
			{
				for (int j = 0; j < Liquid.Volume; j++)
				{
					if (num4.in100())
					{
						num8++;
					}
					if (liquidFluidity.in100())
					{
						num5++;
					}
					else if (num3.in100())
					{
						num7++;
					}
					else if (num2.in100())
					{
						num6++;
					}
				}
			}
			if (num6 > 0)
			{
				Liquid.UseDramsByEvaporativity(num6);
			}
			if (num5 > 0)
			{
				LiquidVolume liquid = Liquid;
				Cell targetCell = cell;
				if (!liquid.FlowIntoCell(num5, targetCell))
				{
					Liquid.UseDrams(num5);
				}
			}
			if (num7 > 0)
			{
				Liquid.Stain(base.Object, num7);
			}
			if (num8 > 0)
			{
				base.Object.GetEffect<LiquidStained>()?.Cleanse(num8);
			}
			if (Liquid.Volume <= 0)
			{
				base.Object.RemoveEffect(this);
			}
			else if ((num5 > 0 || num6 > 0 || num7 > 0) && GlobalConfig.GetBoolSetting("LiquidCoveringsHaveWeight"))
			{
				FlushWeightCaches();
			}
		}
	}

	public override bool WantEvent(int ID, int cascade)
	{
		if (!base.WantEvent(ID, cascade) && ID != PooledEvent<CanBeTradedEvent>.ID && ID != PooledEvent<CheckAnythingToCleanEvent>.ID && ID != PooledEvent<CleanItemsEvent>.ID && ID != SingletonEvent<EndTurnEvent>.ID && ID != PooledEvent<GetDisplayNameEvent>.ID && ID != PooledEvent<GetElectricalConductivityEvent>.ID)
		{
			if (ID == GetExtrinsicWeightEvent.ID)
			{
				return GlobalConfig.GetBoolSetting("LiquidCoveringsHaveWeight");
			}
			return false;
		}
		return true;
	}

	public override bool HandleEvent(CanBeTradedEvent E)
	{
		if (Liquid != null && Liquid.ConsiderLiquidDangerousToContact())
		{
			return false;
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(CheckAnythingToCleanEvent E)
	{
		if (Liquid != null && (Liquid.ComponentLiquids.Count > 2 || Liquid.ConsiderLiquidDangerousToContact() || !Liquid.IsLiquidUsableForCleaning()))
		{
			return false;
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(GetElectricalConductivityEvent E)
	{
		if (E.Pass == 3 && Liquid != null && E.Object == base.Object)
		{
			E.MinValue(Liquid.GetLiquidElectricalConductivity());
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(CleanItemsEvent E)
	{
		E.RegisterObject(base.Object);
		if (Liquid == null)
		{
			E.RegisterType("liquids");
		}
		else
		{
			BaseLiquid primaryLiquid = Liquid.GetPrimaryLiquid();
			if (primaryLiquid != null)
			{
				if (!primaryLiquid.EnableCleaning)
				{
					E.RegisterType(primaryLiquid.GetName(Liquid));
				}
				BaseLiquid secondaryLiquid = Liquid.GetSecondaryLiquid();
				if (secondaryLiquid != null && !secondaryLiquid.EnableCleaning)
				{
					E.RegisterType(secondaryLiquid.GetName(Liquid));
				}
			}
			List<string> tertiaries = Liquid.GetTertiaries();
			if (tertiaries != null)
			{
				foreach (string item in tertiaries)
				{
					BaseLiquid liquid = LiquidVolume.GetLiquid(item);
					if (!liquid.EnableCleaning)
					{
						E.RegisterType(liquid.GetName(Liquid));
					}
				}
			}
			Liquid.FlowIntoCell(-1, base.Object.GetCurrentCell(), E.Actor);
		}
		base.Object.RemoveEffect(this, NeedStackCheck: false);
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(GetExtrinsicWeightEvent E)
	{
		if (Liquid != null && Duration > 0 && GlobalConfig.GetBoolSetting("LiquidCoveringsHaveWeight"))
		{
			E.Weight += Liquid.GetLiquidWeight();
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(EndTurnEvent E)
	{
		if (Liquid != null && Liquid.Volume > 0 && Duration > 0)
		{
			Liquid.SmearOnTick(base.Object, PouredBy, FromCell);
			if (Duration <= 1)
			{
				ProcessDynamics();
			}
			else if (Duration != 9999)
			{
				Duration--;
			}
		}
		else
		{
			base.Object.RemoveEffect(this);
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(GetDisplayNameEvent E)
	{
		if (Liquid != null && Duration > 0)
		{
			Liquid.ProcessSmear(E);
		}
		return base.HandleEvent(E);
	}

	public override bool Render(RenderEvent E)
	{
		if (Duration > 0 && base.Object != null && base.Object.Render != null && Liquid != null && (!base.Object.IsPlayer() || !Options.AlwaysHPColor))
		{
			Liquid.RenderSmear(E, base.Object);
		}
		return true;
	}

	public override void WasUnstackedFrom(GameObject obj)
	{
		base.WasUnstackedFrom(obj);
		if (Liquid == null)
		{
			return;
		}
		LiquidCovered effect = obj.GetEffect<LiquidCovered>();
		if (effect == null || effect.Liquid == null || effect.Liquid.Volume != Liquid.Volume)
		{
			return;
		}
		int count = base.Object.Count;
		int count2 = obj.Count;
		if (count + count2 <= 0)
		{
			return;
		}
		int num = Liquid.Volume * count / (count + count2);
		int num2 = Liquid.Volume * count2 / (count + count2);
		if (num + num2 < Liquid.Volume)
		{
			if (count > count2)
			{
				num++;
				if (num + num2 < Liquid.Volume)
				{
					num2++;
				}
			}
			else
			{
				num2++;
				if (num + num2 < Liquid.Volume)
				{
					num++;
				}
			}
		}
		if (num > 0)
		{
			Liquid.Volume = num;
		}
		else
		{
			Liquid.Empty();
			Duration = 0;
		}
		if (num2 > 0)
		{
			Liquid.Volume = num2;
			return;
		}
		effect.Liquid.Empty();
		effect.Duration = 0;
	}
}
