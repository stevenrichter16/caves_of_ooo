// Unity-free slices of test fixtures whose home files are Presentation-heavy.
// Bodies copied verbatim from the source noted on each member.
using System.IO;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    // The real fixture also snapshots/restores ~25 static fields and builds a
    // GameBootstrap scene object; here it is an inert scope (each coorun
    // invocation is a fresh process, so cross-run static leakage is moot).
    internal sealed class HotbarSaveFixture : System.IDisposable
    {
        public HotbarSaveFixture(bool withInput = true, bool withRenderer = true) { }
        public void Dispose() { }
        // Assets/Tests/EditMode/Gameplay/Save/GameAuditHotbarSelectionTests.cs:76-77
        public static GameSessionState RoundTrip(GameSessionState state)
        {using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;return GameSessionState.Load(new SaveReader(stream,null));}}
    }
}
