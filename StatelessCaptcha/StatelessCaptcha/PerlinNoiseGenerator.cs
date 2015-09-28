using System;

namespace StatelessCaptcha
{
    internal class PerlinNoiseGenerator
    {
        private readonly Random _random = new Random();
        private readonly Tuple<float, float>[] _gradients = new Tuple<float, float>[514];
        private readonly int[] _permutations = new int[514];

        internal PerlinNoiseGenerator()
        {
            for (int i = 0; i < 514; i++)
            {
                var x = 2 * (float)_random.NextDouble() - 1;
                var y = 2 * (float)_random.NextDouble() - 1;

                var magnitude = (float)Math.Sqrt(x * x + y * y);
                _gradients[i] = Tuple.Create(x / magnitude, y / magnitude);
            }

            for (int i = 0; i < 256; i++)
                _permutations[i] = i;

            for (int i = 0; i < 256; i++)
            {
                var j = _random.Next(256);
                var k = _permutations[i];

                _permutations[i] = _permutations[j];
                _permutations[j] = k;
            }

            for (int i = 0; i < 258; i++)
                _permutations[i + 256] = _permutations[i];
        }

        internal float GetNoise(float x, float y)
        {
            while (x < 0) x += 256;
            var bx0 = (int)x % 256;
            var bx1 = (bx0 + 1) % 256;
            var rx0 = x - (int)x;
            var rx1 = rx0 - 1;

            while (y < 0) y += 256;
            var by0 = (int)y % 256;
            var by1 = (by0 + 1) % 256;
            var ry0 = y - (int)y;
            var ry1 = ry0 - 1;
            var i = _permutations[bx0];
            var j = _permutations[bx1];
            var b00 = _permutations[i + by0];
            var b10 = _permutations[j + by0];
            var b01 = _permutations[i + by1];
            var b11 = _permutations[j + by1];
            var sx = rx0 * rx0 * (3 - 2 * rx0);
            var sy = ry0 * ry0 * (3 - 2 * ry0);
            var s = _gradients[b00].Item1 * rx0 + _gradients[b00].Item2 * ry0;
            var t = _gradients[b10].Item1 * rx1 + _gradients[b10].Item2 * ry0;
            var a = s + sx * (t - s);
            var u = _gradients[b01].Item1 * rx0 + _gradients[b01].Item2 * ry1;
            var v = _gradients[b11].Item1 * rx1 + _gradients[b11].Item2 * ry1;
            var b = u + sx * (v - u);

            return a + sy * (b - a);
        }
    }
}
