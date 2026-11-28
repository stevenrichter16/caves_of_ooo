# Conversation placement in the gameplay viewport

The active map camera supplies the center of the dialogue card, including
its sidebar, hotbar and letterbox offsets. The card scales proportionally
when the playable rectangle is narrower or shorter than the card. Pointer
targets use the same card transform after its position or scale changes.

Ordinary Continue at Tally loaded the earned save. Interact and Chat opened
Darret’s rack. The drawn card’s center matched the actual gameplay viewport
on both axes to within one screen pixel. Its bounds stayed inside the map
and the original stock, Ink, HP and tick matched after closing the card.

The two new placement cases failed against the original full-screen center.
All 241 focused cases passed after the change, including a narrow viewport,
viewport movement while open and the transformed reply hitbox.

[Centered rack](centered-rack.png) · [Measured viewport and card](observations.json)
