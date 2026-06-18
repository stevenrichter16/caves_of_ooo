# Feature Plan — On-Demand "Explain This Code" in the Snapjaw Trace Viewer

> Status: **PLANNED** (not yet implemented). Living-doc per CLAUDE.md §1.1.
> Target artifact: `Docs/Traces/snapjaw-attack-trace.html` (single self-contained file).
> Decisions locked (2026-06-18):
> - **Key handling:** bring-your-own-key, in-browser (keeps the single-file artifact).
> - **Grounding mode:** **Citations** (quote-anchored) — every claim tied to a real
>   quoted span of the provided source. This is the strongest anti-fabrication mode.
> - **Sequencing:** write this plan first (this doc), then build.

---

## 1. Goal

Let a developer **highlight a snippet of code** in the trace's source pane and either:
- click **Explain** for an explanation of what that code does and how it relates to
  its surroundings, **or**
- type a **free-form question** about the highlighted code in a text box and get a
  grounded answer.

Both modes share the exact same machinery — grounding bundle, citations, guardrails,
streaming, cache. The only difference is the user-turn content: **Explain** sends a
fixed "explain this selection" instruction; **Ask** sends the developer's question.
Everything in this doc applies to both unless noted.

The non-negotiable requirement is **accuracy / no hallucination**. The whole design
is organized around grounding the model in the real, verbatim source we already have
and tying every statement back to a quoted span of that source.

One-line flow:

```
select code → ✦ Explain  (or)  type a question + ↵ → assemble verbatim grounding
bundle → Claude Messages API (citations enabled, streaming) → render answer with
inline footnotes that link back to the exact quoted source lines
```

---

## 2. Why this page can do it well

The viewer already contains **verbatim source**, which is exactly what grounding needs:

- `step.code` — the on-screen snippet, extracted verbatim from the repo, padded ±9
  lines, with real start line numbers.
- `DEFS` — full method bodies (71 methods) extracted verbatim via brace-matching,
  each with `path`, `start`, and `crumb`.
- `step.note` / `step.ctx` — the trace's own explanations, for narrative alignment.
- breadcrumb — `namespace › Class › method()` for each step.

So an "explain" request never works from a 3-line fragment in isolation: it works
from the selection **embedded in** the full enclosing snippet **plus** the complete
bodies of every method the selection calls. Grounding breadth is the single biggest
hallucination reducer, and we get it for free.

---

## 3. Anti-hallucination strategy (the core)

Layered defense; "non-hallucinated" is **best-effort, not a guarantee** — see §9.

### Layer 1 — Generous, verbatim grounding
The request carries everything needed to answer, between explicit delimiters, with a
hard instruction that outside knowledge of this codebase must not be used. Sources:
the selection, the full `step.code`, the `DEFS` body of every called method, and the
step framing. (Details in §5.)

### Layer 2 — Citations (the chosen primary defense)
The source is sent as **document block(s) with `citations: {enabled: true}`**. The
model returns answer text interleaved with **citation spans** that point at exact
quoted ranges of the provided source. We render those as inline footnotes (e.g.
`…decrements HP¹` → ¹ `CombatSystem.cs:843  hpStat.BaseValue -= amount`).

Why this is the strongest mode: a claim that can't be anchored to a real quoted span
has nowhere to hide — the absence of a citation on a strong assertion is itself a
signal. It is the most direct mechanical link between "what the model said" and "what
the source actually contains."

> Tradeoff already accepted: **citations are incompatible with structured outputs**
> (`output_config.format`). So we do NOT use the JSON-schema mode; the "uncertain"
> behavior below is enforced by prompt instruction instead of by schema.

### Layer 3 — Grounding-first system prompt with an explicit "don't know" channel
Same system prompt for **both** Explain and Ask — only the user turn differs (a fixed
"explain this selection" vs the developer's question). Instruction (verbatim intent):
> Answer the user's request about the highlighted selection using only what is
> determinable from the provided `<source>`. Cite the exact lines you rely on. Refer
> only to identifiers, parameters, and fields that appear in the source. If the
> answer depends on something not in the provided source, say "not shown in the
> provided context" — do not guess. Never describe an API that does not appear in the
> source. If the question cannot be answered from the provided source, say so plainly
> rather than speculating. End with an "Unknowns" section listing anything you could
> not determine from the provided context.

This phrasing makes the "don't know" channel work identically whether the user clicked
Explain or asked a pointed question the source can't answer.

### Layer 4 — Deterministic client-side lint (no LLM)
Before rendering, scan the model's prose for C#-looking identifiers
(`\b[A-Z][A-Za-z0-9_]*\b`, method-call patterns) and check each against the source
bundle we sent. Any symbol **not present** in the bundle gets a ⚠ "unverified — not
in provided source" badge. This catches the rare invention mechanically, independent
of the model. (Cheap, fully offline, runs on every response.)

### Layer 5 — Per-selection cache
Hash the grounding bundle → cache the result. Re-explaining a selection is instant
and free, and makes outputs stable/reproducible for a given selection.

---

## 4. UX flow

1. `selectionchange` / `mouseup` inside `#code` (and the peek modal) → if there's a
   non-empty selection within code, show a floating popover anchored to the selection
   with **two affordances**:
   - a **✦ Explain** button, and
   - a **free-form question box** ("Ask about this code…") with a send button / ↵.
2. Either action → open a result panel (reuse `.datatip` / `.peek` styling): streams
   the answer live; echoes the developer's question at the top (for Ask); shows a
   collapsible "context sent to the model"; renders citation footnotes; renders an
   **Unknowns** section distinctly.
3. **Conversational follow-ups (Ask mode):** the panel keeps a short message history
   *for the current selection* so the developer can ask follow-up questions ("why does
   that matter?", "what calls this?") without re-selecting. History resets when the
   selection changes. The grounding bundle is sent once (cached prefix) and reused
   across turns; follow-up questions are appended as new user turns.
4. States: streaming, and explicit errors — `401` (bad/missing key), `429` (rate
   limit, show `retry-after`), CORS/network (most likely failure; see §6), empty
   question (ignored), and "no key set" → Explain falls back to the offline explainer
   (§7); Ask shows a "set a key to ask questions" prompt (free-form Q&A has no
   non-LLM fallback).
5. A one-time "set your Anthropic API key" nudge + a ⚙ key field.

---

## 5. Context assembly — the grounding bundle

For a selection we build:

| Piece | Source in the page | Purpose |
|---|---|---|
| Selection text + line range | DOM selection within `#code` | What to explain |
| Enclosing snippet (verbatim) | `step.code` + `step.start` | Before/after context, line numbers |
| Breadcrumb | `step.crumb` | Where this lives |
| Called-method bodies (verbatim) | `DEFS[name]` for each `data-def` in/near the selection | Deep "before and after" — real implementations, not guesses |
| Step framing | `step.note`, `step.ctx` | Narrative alignment |

Each source file/snippet becomes one **document block** (citations enabled) so the
model can cite each independently with a stable title like
`CombatSystem.cs:715–921 (ApplyDamage)`. Token budget is a few KB — cheap and fast.

> ⚠ Verify before building: the exact `document` source shape for **plain-text**
> citation documents (custom-content vs `text` source) against current docs — the
> citation document schema is the one API detail most likely to drift.

---

## 6. API integration (BYO-key, in-browser)

- **Endpoint:** `POST https://api.anthropic.com/v1/messages` via browser `fetch`
  (single-file artifact can't bundle the SDK; raw HTTP is the correct choice here).
- **Model:** `claude-opus-4-8` (current, most capable); `claude-haiku-4-5` as a
  cheap/fast toggle.
- **Params:** `thinking: {type:"adaptive"}`, `output_config:{effort:"medium"}`,
  `max_tokens: ~2000`, `stream: true`. (Do **not** send `temperature`/`top_p` — removed
  on Opus 4.8; they 400.)
- **Headers:** `x-api-key`, `anthropic-version: 2023-06-01`, `content-type: application/json`.
- **Key storage:** a ⚙ field; key saved to `localStorage` on the dev's machine only.
  UI caveat shown plainly: *the key lives in your browser — use only on a trusted
  machine; clearing it is one click.*

**CORS — the real constraint.** Calling `api.anthropic.com` from a browser requires
Anthropic's browser-access opt-in (a `anthropic-dangerous-direct-browser-access`-style
header). The **exact current header name and CORS behavior MUST be verified against
live Anthropic docs at build time** — browser-direct access rules change, and the
loaded skill reference did not enumerate this header. If browser-direct access is
unavailable/blocked, the fallback is the optional local proxy (documented but not the
chosen default).

**Streaming:** consume SSE (`content_block_delta` for text; citation deltas arrive on
their own block events) and append to the panel as they land.

---

## 7. Offline / no-key fallback (non-hallucinated by construction)

If no key is set, **Explain** still produces a 100%-grounded, no-LLM answer assembled
from data we already have:
- the `DEFS` signatures/bodies of methods the selection calls,
- the step `ctx`/`note`,
- the breadcrumb and line range.

It restates verbatim facts only, so it cannot hallucinate. It's the graceful-degrade
path and a baseline to compare LLM output against.

---

## 8. Phasing & files

All in `Docs/Traces/snapjaw-attack-trace.html` (no generator changes needed unless we
later embed whole source files for even deeper grounding).

- **Phase 1** — selection popover with **both** the Explain button **and** the
  free-form question box → grounding-bundle assembly → streaming call with **citations
  enabled** → panel with footnotes. BYO-key. Grounding-first prompt + "Unknowns".
- **Phase 2** — Ask-mode conversational follow-ups (per-selection history, cached
  grounding prefix); Layer 4 client-side citation/identifier lint; per-selection
  cache; error states; offline Explain fallback.
- **Phase 3** — polish: cheap/fast model toggle, "context sent" inspector, copy-as-
  markdown, keyboard shortcut, suggested-question chips.

---

## 9. Honesty bounds & open items

- **Not a guarantee.** Layers 1–4 make fabrication rare and *flag the residue*, but an
  LLM can still be confidently wrong about behavior not present in the provided source.
  The "Unknowns" section + the not-found lint keep that visible rather than hidden.
- **Verify at build time:** (a) the browser-access/CORS header for direct
  `api.anthropic.com` calls; (b) the exact `document` source schema for plain-text
  citation documents. Both are the most drift-prone API details and must be checked
  against live docs before coding, not assumed.
- **Security:** an in-browser key is acceptable for a local dev tool used by its owner;
  it is surfaced clearly in the UI. Teams wanting the key off the client can run the
  optional local proxy (to be documented as an appendix when built).

---

## 10. Why citations over structured-output (recorded rationale)

Both were on the table. Structured output (JSON schema with a required `uncertain`
list) is great for forcing shape and is easy to lint. **Citations win on the stated
priority — non-hallucination** — because they bind each statement to an exact quoted
span of real source, which is a stronger guarantee than "the model filled in an
`uncertain` field." The cost is that we can't also use `output_config.format`
(mutually exclusive), so the "Unknowns" behavior moves into the prompt (Layer 3) and
the lint stays deterministic (Layer 4). Decision: citations primary; revisit only if
citation coverage proves too sparse on these short C# snippets.
