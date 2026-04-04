using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders transient projectile, burst, aura, beam, orbit, ring-wave, and chain-arc visuals
    /// on a dedicated tilemap. FX are driven entirely by AsciiFxBus requests from gameplay code.
    /// </summary>
    public class AsciiFxRenderer
    {
        private const float DefaultBurstDuration = 0.18f;
        private const float ParticleLifetime = 0.08f;
        private const float OrbitFrameDuration = 0.03f;

        private readonly Tilemap _tilemap;
        private readonly System.Random _rng = new System.Random(1337);
        private readonly List<ProjectileFxInstance> _projectiles = new List<ProjectileFxInstance>();
        private readonly List<BurstFxInstance> _bursts = new List<BurstFxInstance>();
        private readonly List<ParticleFxInstance> _particles = new List<ParticleFxInstance>();
        private readonly Dictionary<AuraKey, AuraEmitterInstance> _auras =
            new Dictionary<AuraKey, AuraEmitterInstance>();
        private readonly List<BeamFxInstance> _beams = new List<BeamFxInstance>();
        private readonly List<ChargeOrbitFxInstance> _chargeOrbits = new List<ChargeOrbitFxInstance>();
        private readonly List<RingWaveFxInstance> _ringWaves = new List<RingWaveFxInstance>();
        private readonly List<ChainArcFxInstance> _chainArcs = new List<ChainArcFxInstance>();
        private readonly List<ColumnRiseFxInstance> _columnRises = new List<ColumnRiseFxInstance>();

        private Zone _currentZone;
        private bool _hadVisibleFxLastFrame;

        public AsciiFxRenderer(Tilemap tilemap)
        {
            _tilemap = tilemap;
        }

        public bool HasBlockingFx { get; private set; }

        public int ActiveProjectileCount => _projectiles.Count;
        public int ActiveBurstCount => _bursts.Count;
        public int ActiveParticleCount => _particles.Count;
        public int ActiveAuraCount => _auras.Count;
        public int ActiveBeamCount => _beams.Count;
        public int ActiveChargeOrbitCount => _chargeOrbits.Count;
        public int ActiveRingWaveCount => _ringWaves.Count;
        public int ActiveChainArcCount => _chainArcs.Count;
        public int ActiveColumnRiseCount => _columnRises.Count;

        public void SetZone(Zone zone)
        {
            _currentZone = zone;
            ClearAll();
        }

        public void Update(float deltaTime)
        {
            ConsumeRequests();
            UpdateAuras(deltaTime);
            UpdateChargeOrbits(deltaTime);
            UpdateRingWaves(deltaTime);
            UpdateBeams(deltaTime);
            UpdateChainArcs(deltaTime);
            UpdateColumnRises(deltaTime);
            UpdateProjectiles(deltaTime);
            UpdateBursts(deltaTime);
            UpdateParticles(deltaTime);
            Render();
            HasBlockingFx = ComputeHasBlockingFx();
        }

        public void ClearAll()
        {
            _projectiles.Clear();
            _bursts.Clear();
            _particles.Clear();
            _auras.Clear();
            _beams.Clear();
            _chargeOrbits.Clear();
            _ringWaves.Clear();
            _chainArcs.Clear();
            _columnRises.Clear();
            HasBlockingFx = false;
            _hadVisibleFxLastFrame = false;
            _tilemap?.ClearAllTiles();
        }

        private void ConsumeRequests()
        {
            List<AsciiFxRequest> requests = AsciiFxBus.Drain();
            for (int i = 0; i < requests.Count; i++)
            {
                AsciiFxRequest request = requests[i];
                if (request == null)
                    continue;

                if (request.Type == AsciiFxRequestType.AuraStop)
                {
                    RemoveAura(request.Anchor, request.Theme);
                    continue;
                }

                if (_currentZone == null || request.Zone != _currentZone)
                    continue;

                switch (request.Type)
                {
                    case AsciiFxRequestType.Projectile:
                        if (request.Path != null && request.Path.Count > 0)
                        {
                            _projectiles.Add(new ProjectileFxInstance
                            {
                                Theme = request.Theme,
                                Path = request.Path,
                                Trail = request.Trail,
                                BlocksTurnAdvance = request.BlocksTurnAdvance,
                                DelayRemaining = request.Delay,
                                CurrentIndex = 0
                            });
                        }
                        break;

                    case AsciiFxRequestType.Burst:
                        _bursts.Add(new BurstFxInstance
                        {
                            Theme = request.Theme,
                            X = request.X,
                            Y = request.Y,
                            Duration = request.Duration > 0f ? request.Duration : DefaultBurstDuration,
                            BlocksTurnAdvance = request.BlocksTurnAdvance,
                            DelayRemaining = request.Delay
                        });
                        break;

                    case AsciiFxRequestType.AuraStart:
                        if (request.Anchor != null)
                        {
                            _auras[new AuraKey(request.Anchor, request.Theme)] = new AuraEmitterInstance
                            {
                                Zone = request.Zone,
                                Anchor = request.Anchor,
                                Theme = request.Theme
                            };
                        }
                        break;

                    case AsciiFxRequestType.Beam:
                        if (request.Path != null && request.Path.Count > 0)
                        {
                            _beams.Add(new BeamFxInstance
                            {
                                Theme = request.Theme,
                                Path = request.Path,
                                DX = request.DX,
                                DY = request.DY,
                                Duration = request.Duration,
                                BlocksTurnAdvance = request.BlocksTurnAdvance,
                                DelayRemaining = request.Delay
                            });
                        }
                        break;

                    case AsciiFxRequestType.ChargeOrbit:
                        if (request.Anchor != null)
                        {
                            _chargeOrbits.Add(new ChargeOrbitFxInstance
                            {
                                Theme = request.Theme,
                                Anchor = request.Anchor,
                                Zone = request.Zone,
                                Radius = request.Radius,
                                Duration = request.Duration,
                                BlocksTurnAdvance = request.BlocksTurnAdvance,
                                DelayRemaining = request.Delay
                            });
                        }
                        break;

                    case AsciiFxRequestType.RingWave:
                        _ringWaves.Add(new RingWaveFxInstance
                        {
                            Theme = request.Theme,
                            X = request.X,
                            Y = request.Y,
                            MaxRadius = request.MaxRadius,
                            StepDuration = request.StepDuration,
                            BlocksTurnAdvance = request.BlocksTurnAdvance,
                            DelayRemaining = request.Delay
                        });
                        break;

                    case AsciiFxRequestType.ChainArc:
                        if (request.Path != null && request.Path.Count >= 2)
                        {
                            _chainArcs.Add(new ChainArcFxInstance
                            {
                                Theme = request.Theme,
                                Hops = request.Path,
                                HopDuration = request.StepDuration,
                                BlocksTurnAdvance = request.BlocksTurnAdvance,
                                DelayRemaining = request.Delay
                            });
                        }
                        break;

                    case AsciiFxRequestType.ColumnRise:
                        _columnRises.Add(new ColumnRiseFxInstance
                        {
                            Theme = request.Theme,
                            X = request.X,
                            Y = request.Y,
                            Height = request.Height,
                            StepDuration = request.StepDuration,
                            LingerDuration = request.LingerDuration,
                            BlocksTurnAdvance = request.BlocksTurnAdvance,
                            DelayRemaining = request.Delay
                        });
                        break;
                }
            }
        }


        private void UpdateAuras(float deltaTime)
        {
            if (_auras.Count == 0)
                return;

            var toRemove = new List<AuraKey>();
            foreach (KeyValuePair<AuraKey, AuraEmitterInstance> kvp in _auras)
            {
                AuraEmitterInstance aura = kvp.Value;
                if (aura == null || aura.Zone != _currentZone || aura.Anchor == null)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                Cell anchorCell = _currentZone.GetEntityCell(aura.Anchor);
                if (anchorCell == null)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                FxThemeConfig config = GetThemeConfig(aura.Theme);
                if (config.AuraGlyphs.Length == 0 || config.AuraColors.Length == 0)
                    continue;

                aura.SpawnTimer += deltaTime;
                while (aura.SpawnTimer >= config.AuraInterval)
                {
                    aura.SpawnTimer -= config.AuraInterval;
                    int particleCount = 1 + _rng.Next(2);
                    for (int i = 0; i < particleCount; i++)
                        SpawnAuraParticle(anchorCell.X, anchorCell.Y, config);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
                _auras.Remove(toRemove[i]);
        }

        private void SpawnAuraParticle(int centerX, int centerY, FxThemeConfig config)
        {
            if (config.AuraRising)
            {
                // Rising ember: spawns at anchor with slight random x offset, rises upward
                int xOffset = _rng.Next(3) - 1; // -1, 0, or 1
                int x = centerX + xOffset;
                int y = centerY;

                if (_currentZone == null || !_currentZone.InBounds(x, y))
                    return;

                _particles.Add(new ParticleFxInstance
                {
                    X = x,
                    Y = y,
                    Glyph = config.AuraGlyphs[_rng.Next(config.AuraGlyphs.Length)],
                    ColorString = config.AuraColors[_rng.Next(config.AuraColors.Length)],
                    Remaining = config.AuraRiseLifetime,
                    DY = -1, // -1 in game coords = upward on screen
                    MoveInterval = config.AuraRiseInterval
                });
            }
            else
            {
                // Standard static aura particle on a random neighbor cell
                Point offset = AuraOffsets[_rng.Next(AuraOffsets.Length)];
                int x = centerX + offset.X;
                int y = centerY + offset.Y;

                if (_currentZone == null || !_currentZone.InBounds(x, y))
                    return;

                _particles.Add(new ParticleFxInstance
                {
                    X = x,
                    Y = y,
                    Glyph = config.AuraGlyphs[_rng.Next(config.AuraGlyphs.Length)],
                    ColorString = config.AuraColors[_rng.Next(config.AuraColors.Length)],
                    Remaining = ParticleLifetime
                });
            }
        }

        private void UpdateChargeOrbits(float deltaTime)
        {
            for (int i = _chargeOrbits.Count - 1; i >= 0; i--)
            {
                ChargeOrbitFxInstance orbit = _chargeOrbits[i];
                if (orbit.Anchor == null || orbit.Zone != _currentZone || _currentZone.GetEntityCell(orbit.Anchor) == null)
                {
                    _chargeOrbits.RemoveAt(i);
                    continue;
                }

                float activeDelta = ConsumeDelay(ref orbit.DelayRemaining, deltaTime);
                if (orbit.DelayRemaining > 0f)
                    continue;

                orbit.Elapsed += activeDelta;
                if (orbit.Elapsed >= orbit.Duration)
                    _chargeOrbits.RemoveAt(i);
            }
        }

        private void UpdateRingWaves(float deltaTime)
        {
            for (int i = _ringWaves.Count - 1; i >= 0; i--)
            {
                RingWaveFxInstance ring = _ringWaves[i];
                float activeDelta = ConsumeDelay(ref ring.DelayRemaining, deltaTime);
                if (ring.DelayRemaining > 0f)
                    continue;

                ring.Elapsed += activeDelta;
                if (ring.Elapsed >= ring.StepDuration * ring.MaxRadius)
                    _ringWaves.RemoveAt(i);
            }
        }

        private void UpdateBeams(float deltaTime)
        {
            for (int i = _beams.Count - 1; i >= 0; i--)
            {
                BeamFxInstance beam = _beams[i];
                float activeDelta = ConsumeDelay(ref beam.DelayRemaining, deltaTime);
                if (beam.DelayRemaining > 0f)
                    continue;

                beam.Elapsed += activeDelta;
                if (beam.Elapsed >= beam.Duration)
                    _beams.RemoveAt(i);
            }
        }

        private void UpdateChainArcs(float deltaTime)
        {
            for (int i = _chainArcs.Count - 1; i >= 0; i--)
            {
                ChainArcFxInstance arc = _chainArcs[i];
                float activeDelta = ConsumeDelay(ref arc.DelayRemaining, deltaTime);
                if (arc.DelayRemaining > 0f)
                    continue;

                arc.Elapsed += activeDelta;
                if (arc.Elapsed >= arc.HopDuration * (arc.Hops.Count - 1))
                    _chainArcs.RemoveAt(i);
            }
        }

        private void UpdateColumnRises(float deltaTime)
        {
            for (int i = _columnRises.Count - 1; i >= 0; i--)
            {
                ColumnRiseFxInstance col = _columnRises[i];
                float activeDelta = ConsumeDelay(ref col.DelayRemaining, deltaTime);
                if (col.DelayRemaining > 0f)
                    continue;

                col.Elapsed += activeDelta;
                float totalDuration = col.StepDuration * col.Height + col.LingerDuration;
                if (col.Elapsed >= totalDuration)
                    _columnRises.RemoveAt(i);
            }
        }

        private void UpdateProjectiles(float deltaTime)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                ProjectileFxInstance projectile = _projectiles[i];
                if (projectile.Path == null || projectile.Path.Count == 0)
                {
                    _projectiles.RemoveAt(i);
                    continue;
                }

                float activeDelta = ConsumeDelay(ref projectile.DelayRemaining, deltaTime);
                if (projectile.DelayRemaining > 0f)
                    continue;

                FxThemeConfig config = GetThemeConfig(projectile.Theme);
                projectile.StepTimer += activeDelta;

                while (projectile.StepTimer >= config.ProjectileStepTime)
                {
                    projectile.StepTimer -= config.ProjectileStepTime;

                    if (projectile.CurrentIndex < projectile.Path.Count - 1)
                    {
                        projectile.CurrentIndex++;
                        continue;
                    }

                    Point impact = projectile.Path[projectile.Path.Count - 1];
                    _bursts.Add(new BurstFxInstance
                    {
                        Theme = projectile.Theme,
                        X = impact.X,
                        Y = impact.Y,
                        Duration = DefaultBurstDuration,
                        BlocksTurnAdvance = projectile.BlocksTurnAdvance
                    });
                    _projectiles.RemoveAt(i);
                    break;
                }
            }
        }

        private void UpdateBursts(float deltaTime)
        {
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                BurstFxInstance burst = _bursts[i];
                float activeDelta = ConsumeDelay(ref burst.DelayRemaining, deltaTime);
                if (burst.DelayRemaining > 0f)
                    continue;

                burst.Elapsed += activeDelta;
                if (burst.Elapsed >= burst.Duration)
                    _bursts.RemoveAt(i);
            }
        }

        private void UpdateParticles(float deltaTime)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                ParticleFxInstance particle = _particles[i];
                particle.Remaining -= deltaTime;
                if (particle.Remaining <= 0f)
                {
                    _particles.RemoveAt(i);
                    continue;
                }

                // Rising particles move upward periodically
                if (particle.DY != 0 && particle.MoveInterval > 0f)
                {
                    particle.MoveTimer += deltaTime;
                    while (particle.MoveTimer >= particle.MoveInterval)
                    {
                        particle.MoveTimer -= particle.MoveInterval;
                        particle.Y += particle.DY;
                    }
                }
            }
        }

        private void Render()
        {
            if (_tilemap == null)
                return;

            bool hasVisibleFx = _projectiles.Count > 0 || _bursts.Count > 0 || _particles.Count > 0 ||
                                _beams.Count > 0 || _chargeOrbits.Count > 0 || _ringWaves.Count > 0 ||
                                _chainArcs.Count > 0 || _columnRises.Count > 0;
            if (!hasVisibleFx && !_hadVisibleFxLastFrame)
                return;

            _tilemap.ClearAllTiles();

            for (int i = 0; i < _particles.Count; i++)
                RenderParticle(_particles[i]);

            for (int i = 0; i < _ringWaves.Count; i++)
                RenderRingWave(_ringWaves[i]);

            for (int i = 0; i < _chargeOrbits.Count; i++)
                RenderChargeOrbit(_chargeOrbits[i]);

            for (int i = 0; i < _beams.Count; i++)
                RenderBeam(_beams[i]);

            for (int i = 0; i < _chainArcs.Count; i++)
                RenderChainArc(_chainArcs[i]);

            for (int i = 0; i < _columnRises.Count; i++)
                RenderColumnRise(_columnRises[i]);

            for (int i = 0; i < _projectiles.Count; i++)
                RenderProjectile(_projectiles[i]);

            for (int i = 0; i < _bursts.Count; i++)
                RenderBurst(_bursts[i]);

            _hadVisibleFxLastFrame = hasVisibleFx;
        }

        private void RenderChargeOrbit(ChargeOrbitFxInstance orbit)
        {
            if (orbit.DelayRemaining > 0f || orbit.Anchor == null || _currentZone == null)
                return;

            Cell anchorCell = _currentZone.GetEntityCell(orbit.Anchor);
            if (anchorCell == null)
                return;

            FxThemeConfig config = GetThemeConfig(orbit.Theme);
            if (config.ChargeGlyphs.Length == 0 || config.ChargeColors.Length == 0)
                return;

            List<Point> offsets = GetRingOffsets(Math.Max(1, orbit.Radius));
            if (offsets.Count == 0)
                return;

            int phase = Math.Abs((int)(orbit.Elapsed / OrbitFrameDuration)) % offsets.Count;
            int count = Math.Min(4, offsets.Count);
            int step = Math.Max(1, offsets.Count / count);
            for (int i = 0; i < count; i++)
            {
                Point offset = offsets[(phase + (i * step)) % offsets.Count];
                char glyph = config.ChargeGlyphs[(phase + i) % config.ChargeGlyphs.Length];
                string color = config.ChargeColors[(phase + i) % config.ChargeColors.Length];
                RenderGlyphAt(anchorCell.X + offset.X, anchorCell.Y + offset.Y, glyph, color);
            }
        }

        private void RenderRingWave(RingWaveFxInstance ring)
        {
            if (ring.DelayRemaining > 0f)
                return;

            FxThemeConfig config = GetThemeConfig(ring.Theme);
            if (config.RingGlyphs.Length == 0 || config.RingColors.Length == 0)
                return;

            int radius = Mathf.Clamp((int)(ring.Elapsed / ring.StepDuration) + 1, 1, ring.MaxRadius);
            char glyph = config.RingGlyphs[Math.Min(radius - 1, config.RingGlyphs.Length - 1)];
            string color = config.RingColors[Math.Min(radius - 1, config.RingColors.Length - 1)];

            List<Point> offsets = GetRingOffsets(radius);
            for (int i = 0; i < offsets.Count; i++)
                RenderGlyphAt(ring.X + offsets[i].X, ring.Y + offsets[i].Y, glyph, color);
        }

        private void RenderBeam(BeamFxInstance beam)
        {
            if (beam.DelayRemaining > 0f || beam.Path == null || beam.Path.Count == 0)
                return;

            FxThemeConfig config = GetThemeConfig(beam.Theme);
            string[] colors = config.BeamColors.Length > 0 ? config.BeamColors : config.ProjectileColors;
            if (colors.Length == 0)
                colors = DefaultBeamColors;

            char glyph = GetBeamGlyph(beam.DX, beam.DY);
            for (int i = 0; i < beam.Path.Count; i++)
            {
                Point point = beam.Path[i];
                string color = colors[i % colors.Length];
                RenderGlyphAt(point.X, point.Y, glyph, color);
            }
        }

        private void RenderChainArc(ChainArcFxInstance arc)
        {
            if (arc.DelayRemaining > 0f || arc.Hops == null || arc.Hops.Count < 2)
                return;

            FxThemeConfig config = GetThemeConfig(arc.Theme);
            if (config.ChainGlyphs.Length == 0 || config.ChainColors.Length == 0)
                return;

            int hopIndex = Mathf.Clamp((int)(arc.Elapsed / arc.HopDuration), 0, arc.Hops.Count - 2);
            Point start = arc.Hops[hopIndex];
            Point end = arc.Hops[hopIndex + 1];
            List<Point> points = GetLinePoints(start, end);
            for (int i = 0; i < points.Count; i++)
            {
                char glyph = config.ChainGlyphs[(hopIndex + i) % config.ChainGlyphs.Length];
                string color = config.ChainColors[(hopIndex + i) % config.ChainColors.Length];
                RenderGlyphAt(points[i].X, points[i].Y, glyph, color);
            }
        }

        private void RenderColumnRise(ColumnRiseFxInstance col)
        {
            if (col.DelayRemaining > 0f)
                return;

            FxThemeConfig config = GetThemeConfig(col.Theme);
            if (config.ColumnGlyphs == null || config.ColumnGlyphs.Length == 0 ||
                config.ColumnColors == null || config.ColumnColors.Length == 0)
                return;

            // How many cells have appeared so far (bottom-to-top)
            int revealedCells = Mathf.Clamp((int)(col.Elapsed / col.StepDuration) + 1, 0, col.Height);

            // Render each revealed cell: bottom cell is at (X, Y), rising upward (Y-1 in game coords)
            for (int h = 0; h < revealedCells; h++)
            {
                int cellY = col.Y - h; // upward in game coords
                char glyph = config.ColumnGlyphs[Math.Min(h, config.ColumnGlyphs.Length - 1)];
                string color = config.ColumnColors[Math.Min(h, config.ColumnColors.Length - 1)];
                RenderGlyphAt(col.X, cellY, glyph, color);
            }
        }

        private void RenderProjectile(ProjectileFxInstance projectile)
        {
            if (projectile.DelayRemaining > 0f)
                return;

            FxThemeConfig config = GetThemeConfig(projectile.Theme);
            int currentIndex = projectile.CurrentIndex;
            if (currentIndex < 0 || currentIndex >= projectile.Path.Count)
                return;

            if (projectile.Trail)
            {
                for (int i = 0; i < currentIndex; i++)
                {
                    Point trailPoint = projectile.Path[i];
                    RenderGlyphAt(trailPoint.X, trailPoint.Y, config.TrailGlyph, config.TrailColor);
                }
            }

            Point head = projectile.Path[currentIndex];
            char glyph = config.ProjectileGlyphs[currentIndex % config.ProjectileGlyphs.Length];
            string color = config.ProjectileColors[currentIndex % config.ProjectileColors.Length];
            RenderGlyphAt(head.X, head.Y, glyph, color);
        }

        private void RenderBurst(BurstFxInstance burst)
        {
            if (burst.DelayRemaining > 0f)
                return;

            FxThemeConfig config = GetThemeConfig(burst.Theme);
            int frameCount = config.BurstGlyphs.Length;
            if (frameCount == 0)
                return;

            float frameDuration = burst.Duration / frameCount;
            int frame = Mathf.Clamp((int)(burst.Elapsed / frameDuration), 0, frameCount - 1);
            char glyph = config.BurstGlyphs[frame];
            string color = config.BurstColors[Math.Min(frame, config.BurstColors.Length - 1)];

            RenderGlyphAt(burst.X, burst.Y, glyph, color);
            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                int x = burst.X + CardinalOffsets[i].X;
                int y = burst.Y + CardinalOffsets[i].Y;
                RenderGlyphAt(x, y, glyph, color);
            }
        }

        private void RenderParticle(ParticleFxInstance particle)
        {
            RenderGlyphAt(particle.X, particle.Y, particle.Glyph, particle.ColorString);
        }

        private void RenderGlyphAt(int x, int y, char glyph, string colorString)
        {
            if (_tilemap == null || _currentZone == null || !_currentZone.InBounds(x, y))
                return;

            Tile tile = CP437TilesetGenerator.GetTile(glyph);
            if (tile == null)
                return;

            Vector3Int tilePos = new Vector3Int(x, Zone.Height - 1 - y, 0);
            _tilemap.SetTile(tilePos, tile);
            _tilemap.SetTileFlags(tilePos, TileFlags.None);
            _tilemap.SetColor(tilePos, QudColorParser.Parse(colorString));
        }

        private bool ComputeHasBlockingFx()
        {
            for (int i = 0; i < _projectiles.Count; i++)
            {
                if (_projectiles[i].BlocksTurnAdvance)
                    return true;
            }

            for (int i = 0; i < _bursts.Count; i++)
            {
                if (_bursts[i].BlocksTurnAdvance)
                    return true;
            }

            for (int i = 0; i < _beams.Count; i++)
            {
                if (_beams[i].BlocksTurnAdvance)
                    return true;
            }

            for (int i = 0; i < _chargeOrbits.Count; i++)
            {
                if (_chargeOrbits[i].BlocksTurnAdvance)
                    return true;
            }

            for (int i = 0; i < _ringWaves.Count; i++)
            {
                if (_ringWaves[i].BlocksTurnAdvance)
                    return true;
            }

            for (int i = 0; i < _chainArcs.Count; i++)
            {
                if (_chainArcs[i].BlocksTurnAdvance)
                    return true;
            }

            for (int i = 0; i < _columnRises.Count; i++)
            {
                if (_columnRises[i].BlocksTurnAdvance)
                    return true;
            }

            return false;
        }

        private void RemoveAura(Entity anchor, AsciiFxTheme theme)
        {
            if (anchor == null)
                return;

            _auras.Remove(new AuraKey(anchor, theme));
        }

        private static FxThemeConfig GetThemeConfig(AsciiFxTheme theme)
        {
            switch (theme)
            {
                case AsciiFxTheme.Ice:
                    return IceConfig;
                case AsciiFxTheme.Poison:
                    return PoisonConfig;
                case AsciiFxTheme.Arcane:
                    return ArcaneConfig;
                case AsciiFxTheme.Lightning:
                    return LightningConfig;
                case AsciiFxTheme.WellFouled:
                    return WellFouledConfig;
                case AsciiFxTheme.WellClean:
                    return WellCleanConfig;
                case AsciiFxTheme.WellImproved:
                    return WellImprovedConfig;
                case AsciiFxTheme.Campfire:
                    return CampfireConfig;
                case AsciiFxTheme.OvenBroken:
                    return OvenBrokenConfig;
                case AsciiFxTheme.OvenWorking:
                    return OvenWorkingConfig;
                case AsciiFxTheme.OvenImproved:
                    return OvenImprovedConfig;
                case AsciiFxTheme.LanternDark:
                    return LanternDarkConfig;
                case AsciiFxTheme.LanternLit:
                    return LanternLitConfig;
                case AsciiFxTheme.LanternBright:
                    return LanternBrightConfig;
                case AsciiFxTheme.Earth:
                    return EarthConfig;
                case AsciiFxTheme.Water:
                    return WaterConfig;
                case AsciiFxTheme.Holy:
                    return HolyConfig;
                default:
                    return FireConfig;
            }
        }

        private static float ConsumeDelay(ref float delayRemaining, float deltaTime)
        {
            if (delayRemaining <= 0f)
                return deltaTime;
            if (deltaTime <= 0f)
                return 0f;
            if (deltaTime <= delayRemaining)
            {
                delayRemaining -= deltaTime;
                return 0f;
            }

            float remainder = deltaTime - delayRemaining;
            delayRemaining = 0f;
            return remainder;
        }

        private static char GetBeamGlyph(int dx, int dy)
        {
            if (dy == 0)
                return '=';
            if (dx == 0)
                return '|';
            return dx == dy ? '\\' : '/';
        }

        private static List<Point> GetRingOffsets(int radius)
        {
            var offsets = new List<Point>();
            if (radius <= 0)
                return offsets;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (Math.Max(Math.Abs(x), Math.Abs(y)) == radius)
                        offsets.Add(new Point(x, y));
                }
            }

            return offsets;
        }

        private static List<Point> GetLinePoints(Point start, Point end)
        {
            var result = new List<Point>();
            int x0 = start.X;
            int y0 = start.Y;
            int x1 = end.X;
            int y1 = end.Y;
            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                result.Add(new Point(x0, y0));
                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return result;
        }

        private class ProjectileFxInstance
        {
            public AsciiFxTheme Theme;
            public List<Point> Path;
            public bool Trail;
            public bool BlocksTurnAdvance;
            public int CurrentIndex;
            public float StepTimer;
            public float DelayRemaining;
        }

        private class BurstFxInstance
        {
            public AsciiFxTheme Theme;
            public int X;
            public int Y;
            public float Elapsed;
            public float Duration;
            public bool BlocksTurnAdvance;
            public float DelayRemaining;
        }

        private class ParticleFxInstance
        {
            public int X;
            public int Y;
            public char Glyph;
            public string ColorString;
            public float Remaining;
            // Rising particle support
            public int DY;
            public float MoveInterval;
            public float MoveTimer;
        }

        private class AuraEmitterInstance
        {
            public Zone Zone;
            public Entity Anchor;
            public AsciiFxTheme Theme;
            public float SpawnTimer;
        }

        private class BeamFxInstance
        {
            public AsciiFxTheme Theme;
            public List<Point> Path;
            public int DX;
            public int DY;
            public float Elapsed;
            public float Duration;
            public bool BlocksTurnAdvance;
            public float DelayRemaining;
        }

        private class ChargeOrbitFxInstance
        {
            public Zone Zone;
            public Entity Anchor;
            public AsciiFxTheme Theme;
            public int Radius;
            public float Elapsed;
            public float Duration;
            public bool BlocksTurnAdvance;
            public float DelayRemaining;
        }

        private class RingWaveFxInstance
        {
            public AsciiFxTheme Theme;
            public int X;
            public int Y;
            public int MaxRadius;
            public float Elapsed;
            public float StepDuration;
            public bool BlocksTurnAdvance;
            public float DelayRemaining;
        }

        private class ChainArcFxInstance
        {
            public AsciiFxTheme Theme;
            public List<Point> Hops;
            public float Elapsed;
            public float HopDuration;
            public bool BlocksTurnAdvance;
            public float DelayRemaining;
        }

        private class ColumnRiseFxInstance
        {
            public AsciiFxTheme Theme;
            public int X;
            public int Y;
            public int Height;
            public float Elapsed;
            public float StepDuration;
            public float LingerDuration;
            public bool BlocksTurnAdvance;
            public float DelayRemaining;
        }

        private struct AuraKey
        {
            public readonly Entity Anchor;
            public readonly AsciiFxTheme Theme;

            public AuraKey(Entity anchor, AsciiFxTheme theme)
            {
                Anchor = anchor;
                Theme = theme;
            }
        }

        private class FxThemeConfig
        {
            public char[] ProjectileGlyphs;
            public string[] ProjectileColors;
            public char TrailGlyph;
            public string TrailColor;
            public float ProjectileStepTime;
            public char[] BurstGlyphs;
            public string[] BurstColors;
            public char[] AuraGlyphs;
            public string[] AuraColors;
            public float AuraInterval;
            public char[] ChargeGlyphs;
            public string[] ChargeColors;
            public string[] BeamColors;
            public char[] RingGlyphs;
            public string[] RingColors;
            public char[] ChainGlyphs;
            public string[] ChainColors;
            // Rising aura particles: if true, aura particles spawn at the anchor
            // and rise upward (dy=-1 in game coords) instead of static neighbor dots
            public bool AuraRising;
            public float AuraRiseInterval;  // seconds between each upward step
            public float AuraRiseLifetime;  // total particle lifetime (determines rise height)
            // ColumnRise FX: glyphs appear bottom-to-top at a target position
            public char[] ColumnGlyphs;
            public string[] ColumnColors;
        }

        private static readonly string[] DefaultBeamColors = { "&W" };

        private static readonly Point[] CardinalOffsets =
        {
            new Point(0, -1),
            new Point(1, 0),
            new Point(0, 1),
            new Point(-1, 0)
        };

        private static readonly Point[] AuraOffsets =
        {
            new Point(-1, -1),
            new Point(0, -1),
            new Point(1, -1),
            new Point(-1, 0),
            new Point(1, 0),
            new Point(-1, 1),
            new Point(0, 1),
            new Point(1, 1)
        };

        private static readonly FxThemeConfig FireConfig = new FxThemeConfig
        {
            ProjectileGlyphs = new[] { '*', '+', 'x', 'X' },
            ProjectileColors = new[] { "&R", "&Y" },
            TrailGlyph = '.',
            TrailColor = "&R",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = new[] { '*', '+', 'x' },
            BurstColors = new[] { "&R", "&Y", "&W" },
            AuraGlyphs = new[] { '*', '\u00B7', '+' },
            AuraColors = new[] { "&R", "&Y" },
            AuraInterval = 0.15f,
            ChargeGlyphs = new[] { '*', '+', 'o' },
            ChargeColors = new[] { "&R", "&Y" },
            BeamColors = new[] { "&R", "&Y", "&W" },
            RingGlyphs = new[] { '*', '+' },
            RingColors = new[] { "&R", "&Y", "&W" },
            ChainGlyphs = new[] { '*', '+', '~' },
            ChainColors = new[] { "&R", "&Y" },
            ColumnGlyphs = Array.Empty<char>(),
            ColumnColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig IceConfig = new FxThemeConfig
        {
            ProjectileGlyphs = new[] { '*', 'o', '+' },
            ProjectileColors = new[] { "&C", "&Y", "&B" },
            TrailGlyph = '.',
            TrailColor = "&C",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = new[] { '*', '+', 'o' },
            BurstColors = new[] { "&C", "&Y", "&C" },
            AuraGlyphs = new[] { '*', '\u00B7' },
            AuraColors = new[] { "&C", "&B" },
            AuraInterval = 0.25f,
            ChargeGlyphs = new[] { 'o', '*', '.' },
            ChargeColors = new[] { "&C", "&W", "&B" },
            BeamColors = new[] { "&C", "&W", "&B" },
            RingGlyphs = new[] { 'o', '*' },
            RingColors = new[] { "&C", "&W", "&B" },
            ChainGlyphs = new[] { '*', 'o', '+' },
            ChainColors = new[] { "&C", "&W" },
            ColumnGlyphs = Array.Empty<char>(),
            ColumnColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig PoisonConfig = new FxThemeConfig
        {
            ProjectileGlyphs = new[] { 'o', 'O', '*' },
            ProjectileColors = new[] { "&G", "&g", "&y" },
            TrailGlyph = '.',
            TrailColor = "&g",
            ProjectileStepTime = 0.05f,
            BurstGlyphs = new[] { 'o', '*' },
            BurstColors = new[] { "&G", "&g" },
            AuraGlyphs = new[] { 'o', '\u00B7' },
            AuraColors = new[] { "&G", "&g" },
            AuraInterval = 0.20f,
            ChargeGlyphs = new[] { 'o', '*', '.' },
            ChargeColors = new[] { "&G", "&g" },
            BeamColors = new[] { "&G", "&g", "&y" },
            RingGlyphs = new[] { 'o', '*' },
            RingColors = new[] { "&G", "&g" },
            ChainGlyphs = new[] { '~', 'o', '*' },
            ChainColors = new[] { "&G", "&g" },
            ColumnGlyphs = Array.Empty<char>(),
            ColumnColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig ArcaneConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = new[] { "&M", "&C", "&Y" },
            TrailGlyph = '.',
            TrailColor = "&M",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = new[] { '*', 'X', '+' },
            BurstColors = new[] { "&W", "&M", "&Y" },
            AuraGlyphs = new[] { '*', '\u00B7', '+' },
            AuraColors = new[] { "&M", "&C" },
            AuraInterval = 0.20f,
            ChargeGlyphs = new[] { '*', 'o', '+' },
            ChargeColors = new[] { "&M", "&C", "&Y" },
            BeamColors = new[] { "&M", "&W", "&C" },
            RingGlyphs = new[] { '*', 'o', '+' },
            RingColors = new[] { "&M", "&C", "&Y" },
            ChainGlyphs = new[] { '*', '+', 'o' },
            ChainColors = new[] { "&M", "&W", "&C" },
            ColumnGlyphs = Array.Empty<char>(),
            ColumnColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig LightningConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = new[] { "&Y", "&W", "&C" },
            TrailGlyph = '.',
            TrailColor = "&Y",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = new[] { '*', 'Z', '+' },
            BurstColors = new[] { "&W", "&Y" },
            AuraGlyphs = new[] { '*', 'z' },
            AuraColors = new[] { "&Y", "&W" },
            AuraInterval = 0.10f,
            ChargeGlyphs = new[] { '*', 'z', 'Z' },
            ChargeColors = new[] { "&Y", "&W", "&C" },
            BeamColors = new[] { "&Y", "&W", "&C" },
            RingGlyphs = new[] { '*', 'Z', '+' },
            RingColors = new[] { "&Y", "&W" },
            ChainGlyphs = new[] { '~', 'z', 'Z', '*' },
            ChainColors = new[] { "&Y", "&W", "&C" },
            ColumnGlyphs = Array.Empty<char>(),
            ColumnColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig WellFouledConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&y",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = Array.Empty<char>(),
            BurstColors = Array.Empty<string>(),
            AuraGlyphs = new[] { '.' },
            AuraColors = new[] { "&y", "&w" },
            AuraInterval = 0.40f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig WellCleanConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&c",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = Array.Empty<char>(),
            BurstColors = Array.Empty<string>(),
            AuraGlyphs = new[] { '.' },
            AuraColors = new[] { "&c", "&C" },
            AuraInterval = 0.80f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig WellImprovedConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&C",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = Array.Empty<char>(),
            BurstColors = Array.Empty<string>(),
            AuraGlyphs = new[] { '.', '*' },
            AuraColors = new[] { "&C", "&Y" },
            AuraInterval = 0.60f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig CampfireConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&R",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = Array.Empty<char>(),
            BurstColors = Array.Empty<string>(),
            AuraGlyphs = new[] { '.' },
            AuraColors = new[] { "&R", "&Y", "&W" },
            AuraInterval = 0.15f,
            AuraRising = true,
            AuraRiseInterval = 0.12f,  // move up one cell every 0.12s
            AuraRiseLifetime = 0.50f,  // live ~4 cells of rise
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig OvenBrokenConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&K",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = Array.Empty<char>(),
            BurstColors = Array.Empty<string>(),
            AuraGlyphs = new[] { '.' },
            AuraColors = new[] { "&K", "&y" },
            AuraInterval = 0.50f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig OvenWorkingConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&R",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = Array.Empty<char>(),
            BurstColors = Array.Empty<string>(),
            AuraGlyphs = new[] { '.' },
            AuraColors = new[] { "&R", "&Y" },
            AuraInterval = 0.70f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig OvenImprovedConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&Y",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = Array.Empty<char>(),
            BurstColors = Array.Empty<string>(),
            AuraGlyphs = new[] { '.', '*' },
            AuraColors = new[] { "&Y", "&W" },
            AuraInterval = 0.60f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig LanternDarkConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&K",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = Array.Empty<char>(),
            BurstColors = Array.Empty<string>(),
            AuraGlyphs = new[] { '.' },
            AuraColors = new[] { "&K", "&w" },
            AuraInterval = 0.60f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig LanternLitConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&Y",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = Array.Empty<char>(),
            BurstColors = Array.Empty<string>(),
            AuraGlyphs = new[] { '.' },
            AuraColors = new[] { "&Y", "&y" },
            AuraInterval = 0.70f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig LanternBrightConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&W",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = Array.Empty<char>(),
            BurstColors = Array.Empty<string>(),
            AuraGlyphs = new[] { '.', '*' },
            AuraColors = new[] { "&W", "&Y" },
            AuraInterval = 0.50f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>()
        };

        private static readonly FxThemeConfig EarthConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&w",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = new[] { '*', '+', '=' },
            BurstColors = new[] { "&w", "&Y" },
            AuraGlyphs = Array.Empty<char>(),
            AuraColors = Array.Empty<string>(),
            AuraInterval = 999f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>(),
            ColumnGlyphs = new[] { '#', '^', 'A' },
            ColumnColors = new[] { "&w", "&y", "&Y" }
        };

        private static readonly FxThemeConfig WaterConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&b",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = new[] { 'O', 'o', '.' },
            BurstColors = new[] { "&B", "&C", "&Y" },
            AuraGlyphs = Array.Empty<char>(),
            AuraColors = Array.Empty<string>(),
            AuraInterval = 999f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = new[] { '~', '.' },
            RingColors = new[] { "&b", "&B" },
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>(),
            ColumnGlyphs = new[] { '~', '|', '|', '*' },
            ColumnColors = new[] { "&b", "&B", "&C", "&Y" }
        };

        private static readonly FxThemeConfig HolyConfig = new FxThemeConfig
        {
            ProjectileGlyphs = Array.Empty<char>(),
            ProjectileColors = Array.Empty<string>(),
            TrailGlyph = '.',
            TrailColor = "&Y",
            ProjectileStepTime = 0.04f,
            BurstGlyphs = new[] { '+', '*' },
            BurstColors = new[] { "&Y", "&W" },
            AuraGlyphs = new[] { '+', '*' },
            AuraColors = new[] { "&Y", "&W" },
            AuraInterval = 0.30f,
            ChargeGlyphs = Array.Empty<char>(),
            ChargeColors = Array.Empty<string>(),
            BeamColors = Array.Empty<string>(),
            RingGlyphs = Array.Empty<char>(),
            RingColors = Array.Empty<string>(),
            ChainGlyphs = Array.Empty<char>(),
            ChainColors = Array.Empty<string>(),
            ColumnGlyphs = new[] { '#', '+' },
            ColumnColors = new[] { "&Y", "&Y" }
        };
    }
}
