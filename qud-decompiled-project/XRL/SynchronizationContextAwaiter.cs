using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace XRL;

public struct SynchronizationContextAwaiter : INotifyCompletion
{
	private static readonly SendOrPostCallback _postCallback = delegate(object state)
	{
		((Action)state)();
	};

	private readonly SynchronizationContext _context;

	public bool IsCompleted => _context == SynchronizationContext.Current;

	public SynchronizationContextAwaiter(SynchronizationContext context)
	{
		_context = context;
	}

	public void OnCompleted(Action continuation)
	{
		_context.Post(_postCallback, continuation);
	}

	public void GetResult()
	{
	}
}
