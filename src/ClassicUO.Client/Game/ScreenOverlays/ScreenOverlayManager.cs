using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Renderer;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.ScreenOverlays
{
    /// <summary>
    /// Owns and draws the active set of full-screen status overlays (poison, bleed, tunnel vision,
    /// fracture, ...). Skips all work - no texture allocation, no GPU state changes - when the master
    /// toggle is off or nothing is active.
    /// </summary>
    internal sealed class ScreenOverlayManager
    {
        public static ScreenOverlayManager Instance
        {
            get
            {
                field ??= new ScreenOverlayManager();
                return field;
            }
            private set;
        }

        private const int MAX_CONCURRENT = 4;
        private const float MIN_FADE_SECONDS = 0.01f;
        private const float TIME_WRAP_SECONDS = 3600f;

        private sealed class ActiveOverlay
        {
            public ScreenOverlayPreset Preset;
            public OverlayParams Params;
            public int Priority;
            public float Envelope;
            public bool Hiding;
        }

        private readonly Dictionary<OverlayId, ActiveOverlay> _active = new();

        private Texture2D _noiseTexture;
        private ScreenOverlayEffect _effect;
        private float _time;

        /// <summary>
        /// Activates (or re-configures, if already active) an overlay. Fades in from its current
        /// envelope value, never popping. Over the concurrency cap, the lowest-priority active
        /// overlay is evicted to make room.
        /// </summary>
        public void Show(OverlayId id, ScreenOverlayPreset preset, int priority = 0)
        {
            if (_active.TryGetValue(id, out ActiveOverlay existing))
            {
                existing.Preset = preset;
                existing.Params = preset.BakeClamped();
                existing.Priority = priority;
                existing.Hiding = false;
                return;
            }

            if (_active.Count >= MAX_CONCURRENT)
            {
                OverlayId lowest = default;
                int lowestPriority = int.MaxValue;

                foreach (KeyValuePair<OverlayId, ActiveOverlay> kvp in _active)
                {
                    if (kvp.Value.Priority < lowestPriority)
                    {
                        lowestPriority = kvp.Value.Priority;
                        lowest = kvp.Key;
                    }
                }

                _active.Remove(lowest);
            }

            _active[id] = new ActiveOverlay
            {
                Preset = preset,
                Params = preset.BakeClamped(),
                Priority = priority,
                Envelope = 0f,
                Hiding = false
            };
        }

        /// <summary>
        /// Begins fading the overlay out. It keeps drawing (at shrinking intensity) until the fade
        /// completes, then is dropped.
        /// </summary>
        public void Hide(OverlayId id)
        {
            if (_active.TryGetValue(id, out ActiveOverlay o))
                o.Hiding = true;
        }

        public void Draw(UltimaBatcher2D batcher, Rectangle destRect)
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile is not { ScreenOverlaysEnabled: true } || _active.Count == 0)
                return;

            float dt = Time.Delta;
            _time = (_time + dt) % TIME_WRAP_SECONDS;

            AdvanceEnvelopes(dt);

            if (_active.Count == 0)
                return;

            GraphicsDevice gd = batcher.GraphicsDevice;
            EnsureResources(gd);

            Viewport vp = gd.Viewport;
            var ortho = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);
            var screenSize = new Vector2(destRect.Width, destRect.Height);
            float globalIntensity = MathHelper.Clamp(profile.ScreenOverlayIntensity, 0f, 1f);

            batcher.SetBlendState(BlendState.NonPremultiplied);
            batcher.SetSampler(SamplerState.LinearWrap);

            foreach (ActiveOverlay o in _active.Values.OrderBy(o => o.Priority))
            {
                OverlayParams p = o.Params;
                p.Appearance.Intensity *= o.Envelope;

                if (p.Appearance.Intensity <= 0f)
                    continue;

                _effect.CurrentTechnique = _effect.Techniques["T0"];
                _effect.MatrixTransform.SetValue(ortho);
                _effect.Apply(p, _time, screenSize, globalIntensity);

                batcher.Begin(_effect);
                batcher.Draw(_noiseTexture, destRect, Vector3.Zero, 0f);
                batcher.End();
            }

            batcher.SetSampler(null);
            batcher.SetBlendState(null);
        }

        private void AdvanceEnvelopes(float dt)
        {
            List<OverlayId> finishedHiding = null;

            foreach (KeyValuePair<OverlayId, ActiveOverlay> kvp in _active)
            {
                ActiveOverlay o = kvp.Value;

                float target = o.Hiding ? 0f : 1f;
                float fadeSeconds = o.Hiding ? o.Preset.FadeOutSeconds : o.Preset.FadeInSeconds;
                float rate = 1f / MathF.Max(fadeSeconds, MIN_FADE_SECONDS);

                o.Envelope = MoveTowards(o.Envelope, target, rate * dt);

                if (o.Hiding && o.Envelope <= 0f)
                    (finishedHiding ??= new List<OverlayId>()).Add(kvp.Key);
            }

            if (finishedHiding != null)
            {
                foreach (OverlayId id in finishedHiding)
                    _active.Remove(id);
            }
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            if (MathF.Abs(target - current) <= maxDelta)
                return target;

            return current + MathF.Sign(target - current) * maxDelta;
        }

        private void EnsureResources(GraphicsDevice gd)
        {
            _noiseTexture ??= NoiseTextureFactory.Create(gd);
            _effect ??= new ScreenOverlayEffect(gd);
        }
    }
}
