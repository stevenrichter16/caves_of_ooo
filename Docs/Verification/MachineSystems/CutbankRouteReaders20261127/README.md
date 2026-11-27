# Cutbank signs explain the north claim and the free south route

The north boards cross a watched stretch when the post is staffed.
The south bank provides a free dry detour. The direct mire crossing
is shorter and wet. The [route playtest](../CutbankRaisedBanks20261127/README.md)
verified the actual routes and found that the existing signs omitted
or obscured the north claim. Sari-snakes can still interrupt travel.

The shared Examine description corrects the known wording on the
exact west notice and north passage post, including already visited
saves. It preserves the saved text and unrelated authored copy. The
notice describes the watched stretch conditionally so it also fits
older layouts without a staffed post.

Both missing route explanations failed on the old runtime. Three
controls for unrelated roles, coordinates and zones passed. After the
fix, all 172 focused native checks passed. Independent review checked
the source, native results and the matching old text in the archive.

Fresh GameBootstrap Continue opened the pre-fix west fork save at
tick 7230 with 42 HP. Look and Examine selected the original notice
at (18,11). Its saved wording still matched the old text. The live
reader explained the watched boards, the free dry south bank and the
wet crossing. The traveler walked to (34,3) and examined the original
north post at (36,2) before entering watched ground. The post showed
the four-dram fee and the same alternatives. Reading each sign was
free of game-time cost and both original saved descriptions stayed
intact. All 876 checked normal save files and 87 prior tracked edits
matched after the Editor exited Play Mode.

[West notice after Continue](01-west-notice.png) ·
[Passage post after Continue](02-north-post.png) ·
[Native counts and live readouts](observations.json)

## Mac startup

The universal package for `82f5cc2df` built from 32,282 checked
inputs with zero errors and four warnings. GameBootstrap reached
the world on Apple M5. All six native binaries include both
architectures. Both package file records, all 876 checked normal
save files, and startup preferences were preserved. The route and
reader playtests ran in the Editor.
[Startup receipt](mac-startup-verification.json)
