using System.Runtime.CompilerServices;

namespace System.Threading;

public struct UpgradeableReadLockSlim : IDisposable
{
	private ReaderWriterLockSlim Lock;

	public UpgradeableReadLockSlim(ReaderWriterLockSlim Lock)
	{
		this.Lock = Lock;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose()
	{
		Lock.ExitUpgradeableReadLock();
	}
}
