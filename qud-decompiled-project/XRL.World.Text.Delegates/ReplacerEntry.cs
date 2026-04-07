namespace XRL.World.Text.Delegates;

public struct ReplacerEntry
{
	public Replacer Delegate;

	public string Default;

	public int Flags;

	public bool Capitalize;

	public ReplacerEntry(Replacer Delegate, string Default = null, int Flags = 0, bool Capitalize = false)
	{
		this.Delegate = Delegate;
		this.Default = Default;
		this.Flags = Flags;
		this.Capitalize = Capitalize;
	}
}
