using XRL.Collections;

namespace XRL.World;

public class EffectRack : Rack<Effect>
{
	public EffectRack()
	{
	}

	public EffectRack(int Capacity)
		: base(Capacity)
	{
	}

	public void FinalizeRead(GameObject Basis, SerializationReader Reader)
	{
		int i = 0;
		for (int length = Length; i < length; i++)
		{
			Effect effect = Items[i];
			effect.ApplyRegistrar(Basis);
			effect.FinalizeRead(Reader);
			if (length != Length)
			{
				length = Length;
				if (i < length && Items[i] != effect)
				{
					i--;
				}
			}
		}
	}
}
