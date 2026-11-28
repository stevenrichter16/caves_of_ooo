# Conversation popup

The conversation card uses the inventory and trade colors and the
same proportional type. It gives the speaker, optional existing portrait,
faction and disposition their own header. Speech and response choices
have separate space. Each response shows its available shortcut, and
the important actions use plain labels and distinct colors.

The card measures and pages long speech. The visible reply window grows
or shrinks with wrapped choice height. Arrow keys, a controller and the
mouse wheel reach further replies. Clicking uses the same rows the card
draws. Full-screen trade opens through the usual conversation action.

## Actual play

The same earned Tally save opened Darret at tick 5480 before and after
the change. The new greeting shows the complete **Saccharine Concord**
name and lists Trade and Attack once each. The rack shows all three
prices and the longsword's 36-Ink shortfall. Moving the highlight,
opening Trade and closing it return control to the world. The
before and after receipts match for the rental stock, loans, Ink, HP and time.

[Old rack](01-original-rack.png) · [New greeting](02-modern-greeting.png) ·
[New rack](03-modern-rack.png) · [Moved highlight](04-modern-highlight.png) ·
[Trade opened from chat](05-trade-after-chat.png)

Seven new native presentation cases failed on the original. The final
focused run passed 239 tests, including long speech paging, pointer
scroll and selection, keyboard shortcuts and controller selection.
The report audit now reads actual Canvas speech and choice rows.
Independent review checked source, state transitions and the measured
view. The actual Tally route here was played in the Editor.

[Observed labels, preserved state and test totals](observations.json)

## Current Mac app

The Apple M5 package loaded the same saved Tally approach. Ordinary
Interact, Chat, the weapons rack and Trade all opened in sequence.
Closing Trade returned to the world. The saved loan state, normal
saves and preferences, and thirteen retained builds matched their
recorded files. The universal package passed startup and gameplay
on Apple M5. Physical Intel and Steam Deck were outside this pass.

[Standalone conversation](06-mac-rack.png) · [Package and play receipt](mac-package.json)
