using XRL.Collections;

namespace XRL.World;

public class PartRack : Rack<IPart>
{
	public PartRack()
	{
	}

	public PartRack(int Capacity)
		: base(Capacity)
	{
	}

	public void FinalizeRead(GameObject Basis, SerializationReader Reader)
	{
		int i = 0;
		for (int length = Length; i < length; i++)
		{
			IPart part = Items[i];
			part.ApplyRegistrar(Basis);
			part.FinalizeRead(Reader);
			if (length != Length)
			{
				length = Length;
				if (i < length && Items[i] != part)
				{
					i--;
				}
			}
		}
	}
}
