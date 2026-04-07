using System;

namespace Occult.Engine.CodeGeneration;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class GeneratedPartial : Attribute
{
	public ulong Hash;
}
