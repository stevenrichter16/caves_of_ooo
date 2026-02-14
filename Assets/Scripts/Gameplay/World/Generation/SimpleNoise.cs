using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Simple 2D noise for terrain variation.
    /// Simplified port of Qud's PerlinNoise2Df: generates a float[,] field
    /// using cosine interpolation between random samples.
    /// </summary>
    public class SimpleNoise
    {
        private float[,] _baseNoise;
        private float _amplitude;
        private int _frequency;
        private int _baseWidth;
        private int _baseHeight;

        public SimpleNoise(int width, int height, int frequency, float amplitude, System.Random rng)
        {
            _amplitude = amplitude;
            _frequency = Math.Max(1, frequency);
            _baseWidth = width / _frequency + 2;
            _baseHeight = height / _frequency + 2;

            // Generate base random samples
            _baseNoise = new float[_baseWidth, _baseHeight];
            for (int x = 0; x < _baseWidth; x++)
                for (int y = 0; y < _baseHeight; y++)
                    _baseNoise[x, y] = (float)rng.NextDouble();
        }

        /// <summary>
        /// Cosine interpolation between two values.
        /// Port of Qud's PerlinNoise2Df.interpolate.
        /// </summary>
        private static float Interpolate(float a, float b, float t)
        {
            float f = t * (float)Math.PI;
            f = (1f - (float)Math.Cos(f)) * 0.5f;
            return a * (1f - f) + b * f;
        }

        /// <summary>
        /// Get interpolated noise value at a position.
        /// </summary>
        public float Sample(float x, float y)
        {
            float sx = x / _frequency;
            float sy = y / _frequency;

            int ix = (int)sx;
            int iy = (int)sy;
            float fx = sx - ix;
            float fy = sy - iy;

            // Clamp to valid range
            ix = Math.Max(0, Math.Min(ix, _baseWidth - 2));
            iy = Math.Max(0, Math.Min(iy, _baseHeight - 2));

            // Bilinear cosine interpolation
            float top = Interpolate(_baseNoise[ix, iy], _baseNoise[ix + 1, iy], fx);
            float bottom = Interpolate(_baseNoise[ix, iy + 1], _baseNoise[ix + 1, iy + 1], fx);
            return Interpolate(top, bottom, fy) * _amplitude;
        }

        /// <summary>
        /// Generate a 2D noise field by summing multiple octaves.
        /// Returns float[width, height] normalized to [0..1].
        /// </summary>
        public static float[,] GenerateField(int width, int height, System.Random rng, int octaves = 3)
        {
            var field = new float[width, height];

            // Create noise layers at different frequencies
            int[] frequencies = { 4, 16, 64 };
            float[] amplitudes = { 1.0f, 0.7f, 0.4f };
            int layerCount = Math.Min(octaves, frequencies.Length);

            var layers = new SimpleNoise[layerCount];
            for (int i = 0; i < layerCount; i++)
                layers[i] = new SimpleNoise(width, height, frequencies[i], amplitudes[i], rng);

            // Sum all layers
            float maxVal = float.MinValue;
            float minVal = float.MaxValue;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float sum = 0f;
                    for (int i = 0; i < layerCount; i++)
                        sum += layers[i].Sample(x, y);

                    field[x, y] = sum;
                    if (sum > maxVal) maxVal = sum;
                    if (sum < minVal) minVal = sum;
                }
            }

            // Normalize to [0..1]
            float range = maxVal - minVal;
            if (range < 0.0001f) range = 1f;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    field[x, y] = (field[x, y] - minVal) / range;

            return field;
        }
    }
}
