# Machine design reports (2026-11-17/18)

Read-only research produced for the machine architecture and the home/base design. They are inputs, not specifications; the accepted contracts are
[MACHINE-ARCHITECTURE-SPEC.md](../MACHINE-ARCHITECTURE-SPEC.md) (shared machine/object contracts and generic placement, slices S1-S24) and
[HOME-BASE-DESIGN.md](../HOME-BASE-DESIGN.md) (set-down places, slices PB0-PB12). Living implementation records: `MACHINE-INSTALLATIONS.md`,
`MACHINE-STATIONS.md`, `MACHINE-READERS.md`, `MACHINE-ACTION-ROWS.md`, `HOME-BASE-IMPLEMENTATION.md`.

- `machine-*.md`: one V1 dossier per roster machine (forge and rig, still, cradle, assay, vat, capstan, pump, thermal, dynamo, resonator, relay, bath), each with a
  purpose audit, an honest smallest V1, the shared contracts it needs and a failing-test plan. Most conclude that the machine has no demonstrated purpose in the
  shipped world yet and recommend an opportunity-first path; the census ratchet in `MACHINE-INSTALLATIONS.md` follows them.
- `PLACEMENT-PLAN.md`: the every-town placement analysis behind `Installations.json`.
- `critique-*.md`: three independent critiques of that plan (canon and player, feasibility, principles).
- Statements marked [V] were verified against the code at the time, [R] against lore, [I] inferred, [U] unverified; verify again before relying on a line number.
