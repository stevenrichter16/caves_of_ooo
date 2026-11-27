# Slip accepts settled oil for the east lamps

The warden can accept three measures of settled oil from one carried
flask for East of the Well. The same job still accepts three bottles
of lamp oil. The poured oil uses the existing measured handover and
keeps the exact flask, including any remainder. Either payment
advances the saved lamp-supply condition on an ordinary turn. The
player then reports for 10 drams and 60 XP. The chalked names offer
a separate way through the concern and set their own condition.

## Two ordinary handovers

Both branches ran in native GameBootstrap with an isolated seed-65
save. They used supplies earned in ordinary play at different times.

| Branch | Payment | Report and Continue |
| --- | --- | --- |
| Settled oil | The traveler carried three measures from the [Sill still](../MireHeatReadiness20261127/README.md). Brinna Mosswick took exactly three at tick 3600, returned flask 9873 empty and left the separate flask 10242 with twelve brine. | An ordinary step at 3610 completed the supply objective. Reporting raised the purse from 5 to 15 and XP from 31 to 91. Fresh Continue kept the same flasks, condition, report and 5 HP. |
| Bottled lamp oil | A separate branch carried the three original bottles purchased from [Yevven in Sill](../QuillholdDeskLamps20261127/README.md). Brinna accepted and spent all three at tick 2260. | One wait at 2270 completed the supply objective. Reporting raised the purse from 3 to 13 and XP from 0 to 60. Fresh Continue kept the purchase spent, the condition, the report and 40 HP. |

The measured branch took a long route and crossed a sari-snake
encounter. It used the player's existing healing tonic and reached
the report at 5/41 HP. The bottled branch walked a different approach
at a different world time and remained at 40 HP. This is a comparison
of handovers and their consequences. The routes provide no controlled
comparison of safety or the costs of acquiring each supply.

The initial measure quote clipped the end of the flask instruction.
The final shared quote shows the amount, current flask volume and
returned flask on one row. A fresh approach at 23 HP confirmed the
whole row at tick 3540. That reader check made no handover.

After the bottled report, ordinary waiting in a discarded continuation
elicited Brinna's remark about the east lamps at tick 2390. Both saved
reports kept `Slip.EastLampsLit` and left `Slip.NamesSaid` unset.
The condition currently changes local remarks. Physical lighting
is the next part of the project.

## Verification

The original content passed four controls and failed two cases that
required the measured choice. The first connected run found one test
assertion that expected the stale quote to keep its node object.
The existing conversation rebuilds the status node with the correct
remaining volume. The final test asserts the refreshed quote and
conservation. All 178 final focused native checks passed, including
a five-measure flask with two measures returned, split two-plus-two,
wrong liquid, stale selection and one-time report payment.
Independent source and recorded-play reviewers checked each route,
the two saved graphs and the visible dialogue.
All 876 protected normal Quick files and 87 unrelated tracked files
matched their pre-run hashes.

- [State, dialogue and save pins](observations.json)
- [The warden's offer](captures/01-warden-offer.png)
- [Three measures and a returned flask](captures/02-measured-handover.png)
- [Reporting back](captures/03-warden-report.png)
- [Reward and saved result](captures/04-oil-reward.png)
- [A later resident remark](captures/05-resident-remark.png)
