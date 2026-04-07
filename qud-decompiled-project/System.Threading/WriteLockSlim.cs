using System.Runtime.CompilerServices;

namespace System.Threading;

public struct WriteLockSlim : IDisposable
{
	private ReaderWriterLockSlim Lock;

	public WriteLockSlim(ReaderWriterLockSlim Lock)
	{
		this.Lock = Lock;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose()
	{
		Lock.ExitWriteLock();
	}
}
