using System;
using XRL.World;

namespace XRL.Core;

public struct ActionCommandEntry : IComposite
{
	public Guid ID;

	public IActionCommand Command;

	public int SegmentDelay;

	public ActionCommandEntry(Guid ID, IActionCommand Command, int SegmentDelay)
	{
		this.ID = ID;
		this.Command = Command;
		this.SegmentDelay = SegmentDelay;
	}

	public ActionCommandEntry(IActionCommand Action, int SegmentDelay)
		: this(Guid.NewGuid(), Action, SegmentDelay)
	{
	}

	public void Write(SerializationWriter Writer)
	{
		Writer.Write(ID);
		Writer.Write(Command);
		Writer.WriteOptimized(SegmentDelay);
	}

	public void Read(SerializationReader Reader)
	{
		ID = Reader.ReadGuid();
		Command = Reader.ReadComposite() as IActionCommand;
		SegmentDelay = Reader.ReadOptimizedInt32();
	}
}
