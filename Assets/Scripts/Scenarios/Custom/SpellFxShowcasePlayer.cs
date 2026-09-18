using System.Collections.Generic;
using CavesOfOoo.Rendering;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Scenario-only playback controls. No part of this component is save data.</summary>
    public sealed class SpellFxShowcasePlayer : MonoBehaviour
    {
        public bool Paused;
        public float SecondsPerCase = 2.2f;
        public int CurrentCaseIndex { get; private set; } = -1;
        public string CurrentCaseLabel => CurrentStage?.Definition.Label ?? "Preparing";
        public SpellFxShowcase.Stage CurrentStage { get; private set; }
        public bool LastCastAccepted => CurrentStage != null && CurrentStage.Accepted;
        public int CaseCount => _cases?.Count ?? 0;
        private ScenarioContext _context;
        private IReadOnlyList<SpellFxShowcase.CaseDefinition> _cases;
        private ZoneRenderer _renderer;
        private float _elapsed;
        private int _failures;

        public void Initialize(ScenarioContext context)
        {
            _context = context;
            _cases = SpellFxShowcase.CreateCases();
            _renderer = Object.FindFirstObjectByType<ZoneRenderer>();
            // Bootstrap finishes wiring rendering after the scenario callback.
            _elapsed = -0.7f;
        }

        private void Update()
        {
            if (_context == null || Paused) return;
            if (_renderer != null && _renderer.CurrentZone != _context.Zone)
            {
                Destroy(gameObject);
                return;
            }
            _elapsed += Time.unscaledDeltaTime;
            if (CurrentCaseIndex < 0)
            {
                if (_elapsed >= 0f) PlayCase(0);
                return;
            }
            if (_elapsed < SecondsPerCase || (_renderer != null && _renderer.HasBlockingFx)) return;
            if (CurrentCaseIndex + 1 < CaseCount) NextCase();
            else
            {
                Paused = true;
                _context.Log("Spell FX Showcase completed " + CaseCount + " cases; unexpected outcomes: " + _failures + ".");
            }
        }

        /// <summary>Prepare without resolving, for deterministic frame capture or inspection.</summary>
        public void PrepareCase(int index)
        {
            if (_context == null || index < 0 || index >= CaseCount) return;
            _renderer?.CancelWorldFx();
            CurrentCaseIndex = index;
            CurrentStage = SpellFxShowcase.PrepareCase(_context, _cases[index]);
            _renderer?.RenderZone();
            _elapsed = 0f;
        }

        public bool ExecutePrepared()
        {
            if (CurrentStage == null || CurrentStage.HasExecuted) return false;
            bool accepted = CurrentStage.Execute();
            if (accepted != CurrentStage.Definition.ExpectAccepted) _failures++;
            _context.Log((CurrentCaseIndex + 1) + "/" + CaseCount + " " + CurrentCaseLabel
                + (accepted ? " — cast" : " — refused"));
            _renderer?.RenderZone();
            return accepted;
        }

        public void PlayCase(int index)
        {
            PrepareCase(index);
            ExecutePrepared();
        }

        public void NextCase()
        {
            if (CaseCount > 0) PlayCase((CurrentCaseIndex + 1) % CaseCount);
        }

        private void OnGUI()
        {
            if (_context == null) return;
            float width = Mathf.Min(610, Screen.width - 24);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, 8, width, 74), GUI.skin.box);
            GUILayout.Label("Spell FX · " + (CurrentCaseIndex + 1) + "/" + CaseCount + " · " + CurrentCaseLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Paused ? "Play" : "Pause")) Paused = !Paused;
            if (GUILayout.Button("Replay")) PlayCase(Mathf.Max(0, CurrentCaseIndex));
            if (GUILayout.Button("Next")) { Paused = true; NextCase(); }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
