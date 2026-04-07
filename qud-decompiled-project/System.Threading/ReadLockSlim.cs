using System.Runtime.CompilerServices;

namespace System.Threading;

public struct ReadLockSlim : IDisposable
{
	private ReaderWriterLockSlim Lock;

	public ReadLockSlim(ReaderWriterLockSlim Lock)
	{
		this.Lock = Lock;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose()
	{
		Lock.ExitReadLock();
	}
}
