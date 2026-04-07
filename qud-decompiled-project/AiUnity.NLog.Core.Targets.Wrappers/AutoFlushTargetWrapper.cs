using AiUnity.NLog.Core.Common;
using UnityEngine.Scripting;

namespace AiUnity.NLog.Core.Targets.Wrappers;

[Target("AutoFlushWrapper", IsWrapper = true)]
[Preserve]
public class AutoFlushTargetWrapper : WrapperTargetBase
{
	public AutoFlushTargetWrapper()
		: this(null)
	{
	}

	public AutoFlushTargetWrapper(Target wrappedTarget)
	{
		base.WrappedTarget = wrappedTarget;
	}

	protected override void Write(AsyncLogEventInfo logEvent)
	{
		base.WrappedTarget.WriteAsyncLogEvent(logEvent.LogEvent.WithContinuation(AsyncHelpers.PrecededBy(logEvent.Continuation, base.WrappedTarget.Flush)));
	}
}
