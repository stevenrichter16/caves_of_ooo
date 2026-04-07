using XRL.World;

namespace XRL.CharacterBuilds.Qud;

public class QudCustomizeCharacterModule : QudEmbarkBuilderModule<QudCustomizeCharacterModuleData>
{
	public string genderName
	{
		get
		{
			if (base.data.gender != null)
			{
				return base.data.gender.Name;
			}
			return "nonspecific";
		}
	}

	public string pronounSetName
	{
		get
		{
			if (base.data.pronounSet != null)
			{
				return base.data.pronounSet.Name;
			}
			return "<from gender>";
		}
	}

	public override bool shouldBeEnabled()
	{
		return builder?.GetModule<QudSubtypeModule>()?.data?.Subtype != null;
	}

	public override string DataErrors()
	{
		return null;
	}

	public override string DataWarnings()
	{
		return null;
	}

	public override void InitFromSeed(string seed)
	{
	}

	public void setPet(string pet)
	{
		if (base.data == null)
		{
			base.data = new QudCustomizeCharacterModuleData();
		}
		base.data.pet = (string.IsNullOrEmpty(pet) ? null : pet);
		setData(base.data);
	}

	public void setName(string name)
	{
		if (base.data == null)
		{
			base.data = new QudCustomizeCharacterModuleData();
		}
		base.data.name = (string.IsNullOrEmpty(name) ? null : name);
		setData(base.data);
	}

	public override void Init()
	{
		PronounSet.Reinit();
	}

	public override object handleBootEvent(string id, XRLGame game, EmbarkInfo info, object element = null)
	{
		if (id == QudGameBootModule.BOOTEVENT_BEFOREBOOTPLAYEROBJECT)
		{
			string text = info.getData<QudCustomizeCharacterModuleData>()?.name?.Trim();
			if (!string.IsNullOrEmpty(text))
			{
				game.PlayerName = text;
			}
		}
		if (id == QudGameBootModule.BOOTEVENT_AFTERBOOTPLAYEROBJECT)
		{
			GameObject gameObject = element as GameObject;
			if (base.data != null && base.data.pronounSet != null && gameObject != null)
			{
				base.data.pronounSet.Register();
				gameObject.SetPronounSet(base.data.pronounSet);
			}
			if (base.data != null && gameObject != null && base.data.gender != null)
			{
				base.data.gender.Register();
				gameObject.SetGender(base.data.gender);
			}
		}
		return base.handleBootEvent(id, game, info, element);
	}
}
