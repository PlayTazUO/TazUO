using System;
using System.Collections.Generic;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Renderer
{
    public sealed class PixelPicker(bool shortIdBiased)
    {
        private const int INITIAL_DATA_COUNT = 0x40000; // 256kb

        private readonly FastUlongLookupTable<int?> _ids = new(shortIdBiased);
        private readonly List<byte> _data = new(INITIAL_DATA_COUNT); // list<t> access is 10% slower than t[].

        // Width/height packed into one int (both fit in 16 bits for every UO sprite). Stored in a
        // compact lookup instead of the RLE stream so the out-of-bounds fast path - the vast
        // majority of pixel checks - never touches the big _data list and its cache misses.
        private readonly FastUlongLookupTable<int> _dimensions = new(shortIdBiased);

        // Per-texture, per-row jump points into the RLE stream. The stream is a flat run of spans
        // that can cross row boundaries, so a row records the data offset of the span containing
        // its first pixel, that span's start pixel, and its transparency state. Lets Get() resume
        // the span walk at the cursor's row instead of re-walking the whole texture from pixel 0.
        private readonly FastUlongLookupTable<RowInfo[]> _rows = new(shortIdBiased);

        public bool Get(ulong textureId, int x, int y, int extraRange = 0, double scale = 1f)
        {
            int? index = _ids.Get(textureId);
            if (!index.HasValue)
                return false;

            if (scale != 1f)
            {
                x = (int)(x / scale);
                y = (int)(y / scale);
            }

            int packed = _dimensions.Get(textureId);
            int width = packed & 0xFFFF;
            int height = packed >> 16;

            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return false;
            }

            if (extraRange != 0)
            {
                return GetWalk(index.Value, x, y, width, extraRange);
            }

            RowInfo[] rows = _rows.Get(textureId);
            if (rows == null || rows.Length == 0)
            {
                return GetWalk(index.Value, x, y, width, 0);
            }

            ref RowInfo row = ref rows[y];
            int textureIdx = row.DataIndex;
            int current = row.CurrentPixel;
            bool inTransparentSpan = row.InTransparentSpan;
            int target = x + y * width;

            while (current < target)
            {
                int spanLength = ReadIntegerFromData(ref textureIdx);
                current += spanLength;
                if (target < current)
                {
                    return !inTransparentSpan;
                }
                inTransparentSpan = !inTransparentSpan;
            }
            return false;
        }

        /// <summary>
        /// Original full-stream span walk, from the texture's first pixel. Kept for the extraRange
        /// query (its y-window may reach rows before the queried one, so it cannot start at the row
        /// jump point) and as a fallback when no row data was recorded.
        /// </summary>
        private bool GetWalk(int textureIdx, int x, int y, int width, int extraRange)
        {
            // Skip the width/height varints at the head of the texture data.
            ReadIntegerFromData(ref textureIdx);
            ReadIntegerFromData(ref textureIdx);

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
            if (!_ids.Get(textureId).HasValue)
            {
                width = height = 0;
                return;
            }

            int packed = _dimensions.Get(textureId);
            width = packed & 0xFFFF;
            height = packed >> 16;
        }

        public void Set(ulong textureId, int width, int height, ReadOnlySpan<uint> pixels)
        {
            if (_ids.Get(textureId).HasValue)
                return;

            int begin = _data.Count;
            WriteIntegerToData(width);
            WriteIntegerToData(height);

            RowInfo[] rows = height > 0 ? new RowInfo[height] : [];
            if (height > 0)
            {
                rows[0] = new RowInfo(_data.Count, 0, true);
            }

            bool countingTransparent = true;
            int count = 0;
            int spanStart = 0;
            for (int i = 0, len = width * height; i < len; i++)
            {
                if (i != 0 && i % width == 0)
                {
                    // The span currently being counted is the one that contains pixel i (it may
                    // have begun in an earlier row). _data.Count is where its length lands when it
                    // closes, so the walk can resume mid-texture.
                    rows[i / width] = new RowInfo(_data.Count, spanStart, countingTransparent);
                }

                bool isTransparent = pixels[i] == 0;
                if (countingTransparent != isTransparent)
                {
                    WriteIntegerToData(count);
                    countingTransparent = !countingTransparent;
                    count = 0;
                    spanStart = i;
                }
                count += 1;
            }
            WriteIntegerToData(count);
            _ids.Set(textureId, begin);
            _dimensions.Set(textureId, width | (height << 16));
            _rows.Set(textureId, rows);
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

        private readonly struct RowInfo
        {
            public readonly int DataIndex;
            public readonly int CurrentPixel;
            public readonly bool InTransparentSpan;

            public RowInfo(int dataIndex, int currentPixel, bool inTransparentSpan)
            {
                DataIndex = dataIndex;
                CurrentPixel = currentPixel;
                InTransparentSpan = inTransparentSpan;
            }
        }
    }
}
