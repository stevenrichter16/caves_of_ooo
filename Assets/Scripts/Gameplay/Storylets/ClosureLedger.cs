using System.Collections.Generic;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// The closure-ledger (Docs/ENDING-SPINE.md; Lore/11_SecondSpine.md C4).
    /// Every undertaken act is <see cref="Open"/> until it ends in a sentence:
    /// <see cref="Closed"/> (carried through) or <see cref="Refused"/> (ended by
    /// a deliberate, spoken no). An act dropped in silence stays Open; the
    /// ledger never writes it off — a reading names it so the player can go
    /// and end it. The scale is grammatical, not moral.
    /// </summary>
    public enum ClosureState { Open = 0, Closed = 1, Refused = 2 }

    public sealed class ClosureEntry
    {
        public string QuestId;
        public ClosureState State;
        /// <summary>Turn the act was undertaken (−1 when projected from an older save).</summary>
        public int UndertakenTurn = -1;
        /// <summary>Turn the act ended, or −1 while open.</summary>
        public int EndedTurn = -1;
        /// <summary>True when the end was spoken: a completion or an enacted refusal.</summary>
        public bool Spoken;
        public bool IsOpen => State == ClosureState.Open;
    }

    /// <summary>One reading of the ledger: counts and the open acts by id, in stable order.</summary>
    public sealed class ClosureReading
    {
        public int Closed, Refused, Open;
        public readonly List<string> OpenIds = new List<string>();
        /// <summary>No act left unspoken. The practice-path gate reads exactly this.</summary>
        public bool Clean => Open == 0;
    }
}
