using System;
using XRL.Rules;

namespace XRL.World.Parts;

[Serializable]
public class UnityPrefabImposterChance : IPart
{
	public bool VisibleOnly = true;

	public int Chance;

	public string PrefabID;

	public override void Register(GameObject Object, IEventRegistrar Registrar)
	{
		Registrar.Register("EnteredCell");
		base.Register(Object, Registrar);
	}

	public override bool FireEvent(Event E)
	{
		if (E.ID == "EnteredCell")
		{
			if (Stat.RandomCosmetic(1, 100) <= Chance)
			{
				UnityPrefabImposter unityPrefabImposter = new UnityPrefabImposter();
				unityPrefabImposter.PrefabID = PrefabID;
				unityPrefabImposter.VisibleOnly = VisibleOnly;
				ParentObject.AddPart(unityPrefabImposter);
			}
			ParentObject.RemovePart(this);
		}
		return true;
	}
}
