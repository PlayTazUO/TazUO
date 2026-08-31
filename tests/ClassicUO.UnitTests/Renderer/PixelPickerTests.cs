using System;
using System.Collections.Generic;
using ClassicUO.Renderer;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Renderer;

public class PixelPickerTests
{
    private readonly Random _rng = new(12345);

    [Fact]
    public void MatchesReferenceImplementation_ShortBiased()
    {
        MatchRandomTextures(new PixelPicker(true));
    }

    [Fact]
    public void MatchesReferenceImplementation_LongBiased()
    {
        MatchRandomTextures(new PixelPicker(false));
    }

    [Fact]
    public void HandlesDegenerateTextures()
    {
        RunDegenerateCase(1, 1, [0u]);
        RunDegenerateCase(1, 1, [0xFFFFFFFFu]);
        RunDegenerateCase(64, 64, new uint[64 * 64]); // fully transparent
        RunDegenerateCase(64, 64, Fill(64 * 64, 0xFFFFFFFFu)); // fully opaque
    }

    [Fact]
    public void RepeatedSetIsIgnored()
    {
        var picker = new PixelPicker(true);
        uint[] first = new uint[20 * 20];
        for (int i = 5; i < 15; i++)
        {
            first[i] = 0xFFFFFFFFu; // opaque run on row 0
        }

        uint[] second = new uint[20 * 20]; // all transparent
        picker.Set(1, 20, 20, first);
        picker.Set(1, 20, 20, second); // must be a no-op

        picker.Get(1, 11, 0).Should().BeTrue(); // strictly inside the opaque run
        picker.GetDimensions(1, out int w, out int h);
        Assert.Equal(20, w);
        Assert.Equal(20, h);
    }

    private void RunDegenerateCase(int w, int h, uint[] pixels)
    {
        var picker = new PixelPicker(true);
        var reference = new ReferencePicker();
        picker.Set(7, w, h, pixels);
        reference.Set(7, w, h, pixels);

        for (int y = -2; y <= h + 2; y++)
        {
            for (int x = -2; x <= w + 2; x++)
            {
                Assert.Equal(reference.Get(7, x, y), picker.Get(7, x, y));
            }
        }
    }

    private void MatchRandomTextures(PixelPicker picker)
    {
        var reference = new ReferencePicker();

        for (int t = 0; t < 60; t++)
        {
            int w = _rng.Next(1, 140);
            int h = _rng.Next(1, 140);
            uint[] pixels = new uint[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = _rng.Next(4) == 0 ? 0u : 0xFFFFFFFFu;
            }

            ulong id = (ulong)(1000 + t);
            picker.Set(id, w, h, pixels);
            reference.Set(id, w, h, pixels);

            picker.GetDimensions(id, out int pw, out int ph);
            reference.GetDimensions(id, out int rw, out int rh);
            Assert.Equal(rw, pw);
            Assert.Equal(rh, ph);

            for (int q = 0; q < 3000; q++)
            {
                int x = _rng.Next(-6, w + 6);
                int y = _rng.Next(-6, h + 6);
                int extra = _rng.Next(3) == 0 ? _rng.Next(1, 3) : 0;
                double scale = _rng.Next(4) == 0 ? 0.5 : 1.0;

                Assert.Equal(
                    reference.Get(id, x, y, extra, scale),
                    picker.Get(id, x, y, extra, scale)
                );
            }
        }
    }

    private static uint[] Fill(int count, uint value)
    {
        uint[] result = new uint[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = value;
        }
        return result;
    }

    /// <summary>
    /// Faithful copy of the pre-optimization PixelPicker RLE stream logic, used as the correctness
    /// oracle for the row-jump fast path.
    /// </summary>
    private sealed class ReferencePicker
    {
        private readonly Dictionary<ulong, int> _ids = new();
        private readonly List<byte> _data = new();

        public bool Get(ulong textureId, int x, int y, int extraRange = 0, double scale = 1f)
        {
            if (!_ids.TryGetValue(textureId, out int textureIdx))
            {
                return false;
            }

            if (scale != 1f)
            {
                x = (int)(x / scale);
                y = (int)(y / scale);
            }

            int width = ReadIntegerFromData(ref textureIdx);

            if (x < 0 || x >= width)
            {
                return false;
            }

            if (y < 0 || y >= ReadIntegerFromData(ref textureIdx))
            {
                return false;
            }

            int current = 0;
            int target = x + y * width;
            bool inTransparentSpan = true;
            while (current < target)
            {
                int spanLength = ReadIntegerFromData(ref textureIdx);
                current += spanLength;
                if (extraRange == 0)
                {
                    if (target < current)
                    {
                        return !inTransparentSpan;
                    }
                }
                else
                {
                    if (!inTransparentSpan)
                    {
                        int y0 = current / width;
                        int x1 = current % width;
                        int x0 = x1 - spanLength;
                        for (int range = -extraRange; range <= extraRange; range++)
                        {
                            if (y + range == y0 && (x + extraRange >= x0) && (x - extraRange <= x1))
                            {
                                return true;
                            }
                        }
                    }
                }
                inTransparentSpan = !inTransparentSpan;
            }
            return false;
        }

        public void GetDimensions(ulong textureId, out int width, out int height)
        {
            if (!_ids.TryGetValue(textureId, out int textureIdx))
            {
                width = height = 0;
                return;
            }

            width = ReadIntegerFromData(ref textureIdx);
            height = ReadIntegerFromData(ref textureIdx);
        }

        public void Set(ulong textureId, int width, int height, ReadOnlySpan<uint> pixels)
        {
            if (_ids.ContainsKey(textureId))
            {
                return;
            }

            int begin = _data.Count;
            WriteIntegerToData(width);
            WriteIntegerToData(height);
            bool countingTransparent = true;
            int count = 0;
            for (int i = 0, len = width * height; i < len; i++)
            {
                bool isTransparent = pixels[i] == 0;
                if (countingTransparent != isTransparent)
                {
                    WriteIntegerToData(count);
                    countingTransparent = !countingTransparent;
                    count = 0;
                }
                count += 1;
            }
            WriteIntegerToData(count);
            _ids[textureId] = begin;
        }

        private void WriteIntegerToData(int value)
        {
            while (value > 0x7f)
            {
                _data.Add((byte)((value & 0x7f) | 0x80));
                value >>= 7;
            }
            _data.Add((byte)value);
        }

        private int ReadIntegerFromData(ref int index)
        {
            int value = 0;
            int shift = 0;
            while (true)
            {
                byte data = _data[index++];
                value += (data & 0x7f) << shift;
                if ((data & 0x80) == 0x00)
                {
                    return value;
                }
                shift += 7;
            }
        }
    }
}
