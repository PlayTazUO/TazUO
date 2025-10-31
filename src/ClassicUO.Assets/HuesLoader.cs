// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class HuesLoader : UOFileLoader
    {
        public HuesLoader(UOFileManager fileManager) : base(fileManager)
        {
        }

        public HuesGroup[] HuesRange { get; private set; }

        public int HuesCount { get; private set; }

        public FloatHues[] Palette { get; private set; }

        public ushort[] RadarCol { get; private set; }

        public override unsafe void Load()
        {
            string path = FileManager.GetUOFilePath("hues.mul");

            FileSystemHelper.EnsureFileExists(path);

            using var file = new UOFileMul(path);
            int groupSize = Unsafe.SizeOf<HuesGroup>();
            int entrycount = (int)file.Length / groupSize;
            HuesCount = entrycount * 8;
            HuesRange = new HuesGroup[entrycount];

            for (int i = 0; i < entrycount; i++)
            {
                HuesRange[i] = file.Read<HuesGroup>();
            }

            path = FileManager.GetUOFilePath("radarcol.mul");

            FileSystemHelper.EnsureFileExists(path);

            using var radarcol = new UOFileMul(path);
            RadarCol = new ushort[radarcol.Length / sizeof(ushort)];
            radarcol.Read(MemoryMarshal.AsBytes<ushort>(RadarCol.AsSpan()));
        }

        public float[] CreateHuesPalette()
        {
            float[] p = new float[32 * 3 * HuesCount];

            Palette = new FloatHues[HuesCount];
            int entrycount = HuesCount >> 3;

            for (int i = 0; i < entrycount; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int idx = i * 8 + j;

                    Palette[idx].Palette = new float[32 * 3];

                    for (int h = 0; h < 32; h++)
                    {
                        int idx1 = h * 3;

                        ushort c = HuesRange[i].Entries[j].ColorTable[h];

                        Palette[idx].Palette[idx1] = ((c >> 10) & 0x1F) / 31.0f;

                        Palette[idx].Palette[idx1 + 1] = ((c >> 5) & 0x1F) / 31.0f;

                        Palette[idx].Palette[idx1 + 2] = (c & 0x1F) / 31.0f;

                        p[idx * 96 + idx1 + 0] = Palette[idx].Palette[idx1];

                        p[idx * 96 + idx1 + 1] = Palette[idx].Palette[idx1 + 1];

                        p[idx * 96 + idx1 + 2] = Palette[idx].Palette[idx1 + 2];

                        //p[iddd++] = Palette[idx].Palette[idx1];
                        //p[iddd++] = Palette[idx].Palette[idx1 + 1];
                        //p[iddd++] = Palette[idx].Palette[idx1 + 2];
                    }
                }
            }

            return p;
        }

        public void CreateShaderColors(uint[] buffer)
        {
            int len = HuesRange.Length;

            int idx = 0;

            for (int r = 0; r < len; r++)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        buffer[idx++] = HuesHelper.Color16To32(HuesRange[r].Entries[y].ColorTable[x]) | 0xFF_00_00_00;

                        if (idx >= buffer.Length)
                        {
                            return;
                        }
                    }
                }
            }
        }

        /* Look up the hue and return the color for the given index. Index must be between 0 and 31.
         * The returned color is a 16 bit color in R5B5G5A1 format. */
        public ushort GetHueColorRgba5551(ushort index, ushort hue)
        {
            if (hue != 0 && hue < HuesCount)
            {
                hue -= 1;
                int g = hue >> 3;
                int e = hue % 8;

                return (ushort)(0x8000 | HuesRange[g].Entries[e].ColorTable[index]);
            }

            return 0x8000;
        }

        /* Look up the hue and return the color for the given index. Index must be between 0 and 31.
         * The returned color is a 32 bit color in R8G8B8A8 format. */
        public uint GetHueColorRgba8888(ushort index, ushort hue) => HuesHelper.Color16To32(GetHueColorRgba5551(index, hue));

        /* Apply the hue to the given gray color, returning a 16 bit color. */
        public ushort ApplyHueRgba5551(ushort gray, ushort hue) => GetHueColorRgba5551((ushort)((gray >> 10) & 0x1F), hue);

        /* Apply the hue to the given gray color, returning a 32 bit color. */
        public uint ApplyHueRgba8888(ushort gray, ushort hue) => HuesHelper.Color16To32(ApplyHueRgba5551(gray, hue));

        public ushort GetColor16(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color >> 3;
                int e = color % 8;

                return HuesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F];
            }

            return c;
        }

        public uint GetPolygoneColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color >> 3;
                int e = color % 8;

                return HuesHelper.Color16To32(HuesRange[g].Entries[e].ColorTable[c]);
            }

            return 0xFF010101;
        }

        public uint GetUnicodeFontColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color >> 3;
                int e = color % 8;

                return HuesRange[g].Entries[e].ColorTable[8];
            }

            return HuesHelper.Color16To32(c);
        }

        public uint GetColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color >> 3;
                int e = color % 8;

                return HuesHelper.Color16To32(HuesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F]);
            }

            return color != 0 ? HuesHelper.Color16To32(color) : HuesHelper.Color16To32(c);
        }

        public uint GetPartialHueColor(ushort color, ushort hue)
        {
            uint cl = HuesHelper.Color16To32(color);
            byte R = (byte)(cl & 0xFF);
            byte G = (byte)((cl >> 8) & 0xFF);
            byte B = (byte)((cl >> 16) & 0xFF);

            if (R != G || R != B)
            {
                /* Not gray. Don't apply hue. */
                return HuesHelper.Color16To32(color);
            }

            if (hue == 0 || hue >= HuesCount)
            {
                /* Invalid hue. */
                return HuesHelper.Color16To32(color);
            }

            return ApplyHueRgba8888(color, hue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetRadarColorData(int c)
        {
            if (c >= 0 && c < RadarCol.Length)
            {
                return RadarCol[c];
            }

            return 0;
        }
    }


    [InlineArray(32)]
    public struct ColorTableArray
    {
        private ushort _a0;
    }

    [InlineArray(8)]
    public struct HuesBlockArray
    {
        private HuesBlock _a0;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HuesBlock
    {
        public ColorTableArray ColorTable;
        public ushort TableStart;
        public ushort TableEnd;
        public unsafe fixed byte Name[20];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HuesGroup
    {
        public uint Header;
        public HuesBlockArray Entries;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VerdataHuesBlock
    {
        public ColorTableArray ColorTable;
        public ushort TableStart;
        public ushort TableEnd;
        public unsafe fixed byte Name[20];
        public unsafe fixed ushort Unk[20];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VerdataHuesGroup
    {
        public readonly uint Header;
        public HuesBlockArray Entries;
    }

    public struct FloatHues
    {
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public float[] Palette;
    }
}