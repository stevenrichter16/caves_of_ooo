using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders transient projectile, burst, and aura visuals on a dedicated tilemap.
    /// FX are driven entirely by AsciiFxBus requests from gameplay code.
    /// </summary>
    public class AsciiFxRenderer
    {
        private const float DefaultBurstDuration = 0.18f;
        private const float ParticleLifetime = 0.08f;

        private readonly Tilemap _tilemap;
        private readonly System.Random _rng = new System.Random(1337);
        private readonly List<ProjectileFxInstance> _projectiles = new List<ProjectileFxInstance>();
        private readonly List<BurstFxInstance> _bursts = new List<BurstFxInstance>();
        private readonly List<ParticleFxInstance> _particles = new List<ParticleFxInstance>();
        private readonly Dictionary<AuraKey, AuraEmitterInstance> _auras =
            new Dictionary<AuraKey, AuraEmitterInstance>();

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

        public void SetZone(Zone zone)
        {
            _currentZone = zone;
            ClearAll();
        }

        public void Update(float deltaTime)
        {
            ConsumeRequests();
            UpdateAuras(deltaTime);
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
                            Duration = DefaultBurstDuration,
                            BlocksTurnAdvance = request.BlocksTurnAdvance
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

                FxThemeConfig config = GetThemeConfig(projectile.Theme);
                projectile.StepTimer += deltaTime;

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
                burst.Elapsed += deltaTime;
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
                    _particles.RemoveAt(i);
            }
        }

        private void Render()
        {
            if (_tilemap == null)
                return;

            bool hasVisibleFx = _projectiles.Count > 0 || _bursts.Count > 0 || _particles.Count > 0;
            if (!hasVisibleFx && !_hadVisibleFxLastFrame)
                return;

            _tilemap.ClearAllTiles();

            for (int i = 0; i < _particles.Count; i++)
                RenderParticle(_particles[i]);

            for (int i = 0; i < _projectiles.Count; i++)
                RenderProjectile(_projectiles[i]);

            for (int i = 0; i < _bursts.Count; i++)
                RenderBurst(_bursts[i]);

            _hadVisibleFxLastFrame = hasVisibleFx;
        }

        private void RenderProjectile(ProjectileFxInstance projectile)
        {
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
                default:
                    return FireConfig;
            }
        }

        private class ProjectileFxInstance
        {
            public AsciiFxTheme Theme;
            public List<Point> Path;
            public bool Trail;
            public bool BlocksTurnAdvance;
            public int CurrentIndex;
            public float StepTimer;
        }

        private class BurstFxInstance
        {
            public AsciiFxTheme Theme;
            public int X;
            public int Y;
            public float Elapsed;
            public float Duration;
            public bool BlocksTurnAdvance;
        }

        private class ParticleFxInstance
        {
            public int X;
            public int Y;
            public char Glyph;
            public string ColorString;
            public float Remaining;
        }

        private class AuraEmitterInstance
        {
            public Zone Zone;
            public Entity Anchor;
            public AsciiFxTheme Theme;
            public float SpawnTimer;
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
        }

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
            AuraInterval = 0.15f
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
            AuraGlyphs = Array.Empty<char>(),
            AuraColors = Array.Empty<string>(),
            AuraInterval = 999f
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
            AuraInterval = 0.20f
        };
    }
}
