namespace XRL.World.Text;

public struct TextArgument
{
	public GameObject Object;

	public string Explicit;

	public IPronounProvider Pronouns;

	public TextArgument(GameObject Object, string Explicit = null, IPronounProvider Pronouns = null)
	{
		this.Object = Object;
		this.Explicit = Explicit;
		this.Pronouns = Pronouns;
	}

	public TextArgument(string Explicit, IPronounProvider Pronouns = null)
		: this(null, Explicit, Pronouns)
	{
	}

	public TextArgument(string Explicit, bool ExplicitPlural)
		: this(null, Explicit, ExplicitPlural)
	{
	}

	public TextArgument(GameObject Object, string Explicit, bool ExplicitPlural = false)
		: this(Object, Explicit, ExplicitPlural ? Gender.DefaultPlural : Gender.DefaultNeuter)
	{
	}
}
