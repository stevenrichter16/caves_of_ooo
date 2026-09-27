# Transient capacity turnover

Root's cold-eye review found a concrete order-of-operations defect. Volumes enforce their 512 limits before removing retired views, so a full previous set can prevent every current source from being admitted during its only dirty refresh. The frame adapter enforces its limit before EndFrame retires prior cells, so a full old frame can force current-frame ASCII fallback unnecessarily.

Native test-first fixture uses actual512 capacity, without a test-only capacity override. Exact512/513 gas, element and particle boundaries provide6 controls. Three full-set turnover cases require replacement owners/cells to appear on the same Refresh/frame. The source test has not run natively yet; root owns that RED gate.

After actual RED, candidate scope is limited to lifecycle admission. Retire invalid gas owners/element cells before admission (read-only source checks, no gameplay updates). For particles, snapshot prior keys once at BeginFrame, then recycle only an old view whose key has not been seen this frame, using a monotonically increasing cursor; retain the exact512 live-view limit and original first-admitted/last-draw semantics. A view already used by this frame cannot be recycled. Numeric/unsupported later draw still removes an earlier native mark and uses the normal UI path. Avoid a full dictionary scan per admission and avoid destroying/recreating all views on every frame.

The 512 gas,512 tile and512 distinct decorative-cell limits remain explicit performance bounds. Excess simultaneously current effects retain their old fallback. This is bounded graphical coverage of eligible current sources, not a claim of unlimited native submissions. No timers, spell impacts, state transitions, density, collision, priorities or asset files change.
