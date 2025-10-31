// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Assets;


// ( •_•)>⌐■-■
// https://github.com/cbnolok/UOETE/blob/master/src/uotileart.cpp
public sealed class TileArtLoader : UOFileLoader
{
    private readonly Dictionary<uint, TileArtInfo> _tileArtInfos = [];
    private UOFileUop _file;

    public TileArtLoader(UOFileManager fileManager) : base(fileManager)
    {

    }


    public bool TryGetTileArtInfo(uint graphic, out TileArtInfo tileArtInfo)
    {
        if (_tileArtInfos.TryGetValue(graphic, out tileArtInfo))
            return true;

        if (LoadEntry(graphic, out tileArtInfo))
        {
            _tileArtInfos.Add(graphic, tileArtInfo);
            return true;
        }

        return false;
    }

    private bool LoadEntry(uint graphic, out TileArtInfo tileArtInfo)
    {
        tileArtInfo = null;
        if (_file == null)
            return false;

        ref UOFileIndex entry = ref _file.GetValidRefEntry((int)graphic);
        if (entry.Length == 0)
            return false;

        byte[] buf = ArrayPool<byte>.Shared.Rent(entry.Length);
        byte[] dbuf = ArrayPool<byte>.Shared.Rent(entry.DecompressedLength);

        try
        {
            Span<byte> bufSpan = buf.AsSpan(0, entry.Length);
            Span<byte> dbufSpan = dbuf.AsSpan(0, entry.DecompressedLength);

            _file.Seek(entry.Offset, SeekOrigin.Begin);
            _file.Read(bufSpan);

            ZLib.ZLibError result = ZLib.Decompress(bufSpan, dbufSpan);
            if (result != ZLib.ZLibError.Ok)
            {
                return false;
            }

            var reader = new StackDataReader(dbufSpan);
            tileArtInfo = new(ref reader);

            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
            ArrayPool<byte>.Shared.Return(dbuf);
        }
    }

    public override void Load()
    {
        string path = FileManager.GetUOFilePath("tileart.uop");
        if (!File.Exists(path))
            return;

        _file = new UOFileUop(path, "build/tileart/{0:D8}.bin");
        _file.FillEntries();
    }
}

[Flags]
public enum TAEFlag : ulong
{
    None = 0x0,  //0uL,
    Background = 0x1,  //1uL,
    Weapon = 0x2,  //2uL,
    Transparent = 0x4,  //4uL,
    Translucent = 0x8,  //8uL,
    Wall = 0x10, //16uL,
    Damaging = 0x20, //32uL,
    Impassable = 0x40, //64uL,
    Wet = 0x80, //128uL,
    Ignored = 0x100,    //256uL,
    Surface = 0x200,    //512uL,
    Bridge = 0x400,    //1024uL,
    Generic = 0x800,    //2048uL,
    Window = 0x1000,   //4096uL,
    NoShoot = 0x2000,   //8192uL,
    ArticleA = 0x4000,   //16384uL,
    ArticleAn = 0x8000,   //32768uL,
    ArticleThe = ArticleA | ArticleAn,  //49152uL,
    Mongen = 0x10000,  //65536uL,
    Foliage = 0x20000,  //131072uL,
    PartialHue = 0x40000,  //262144uL,
    UseNewArt = 0x80000,      //524288uL,
    Map = 0x100000,     //1048576uL,
    Container = 0x200000,     //2097152uL,
    Wearable = 0x400000,     //4194304uL,
    LightSource = 0x800000,     //8388608uL,
    Animation = 0x1000000,    //16777216uL,
    HoverOver = 0x2000000,    //33554432uL,
    ArtUsed = 0x4000000,    //67108864uL,
    Armor = 0x8000000,    //134217728uL,
    Roof = 0x10000000,   //268435456uL,
    Door = 0x20000000,   //536870912uL,
    StairBack = 0x40000000,   //1073741824uL,
    StairRight = 0x80000000,   //2147483648uL,
    NoHouse = 0x100000000,  //4294967296uL,
    NoDraw = 0x200000000,  //8589934592uL,
    Unused1 = 0x400000000,  //17179869184uL,
    AlphaBlend = 0x800000000,  //34359738368uL,
    NoShadow = 0x1000000000, //68719476736uL,
    PixelBleed = 0x2000000000, //137438953472uL,
    Unused2 = 0x4000000000, //274877906944uL,
    PlayAnimOnce = 0x8000000000, //549755813888uL,
    MultiMovable = 0x10000000000 //1099511627776uL
};

public enum TAEPropID : byte
{
    Weight = 0,
    Quality,
    Quantity,
    Height,
    Value,
    AcVc,
    Slot,
    Off_C8,
    Appearance,
    Race,
    Gender,
    Paperdoll
}

public sealed class TileArtInfo
{
    internal TileArtInfo(ref StackDataReader reader)
    {
        ushort version = reader.ReadUInt16LE();
        if (version != 4)
        {
            Log.Info($"tileart.uop v{version} is not supported.");
            return;
        }

        uint stringDictOffset = reader.ReadUInt32LE();
        TileId = version >= 4 ? reader.ReadUInt32LE() : reader.ReadUInt16LE();
        bool unkBool1 = reader.ReadBool();
        bool unkBool2 = reader.ReadBool();
        uint unkFloat1 = reader.ReadUInt32LE();
        uint unkFloat2 = reader.ReadUInt32LE();
        uint fixedZero = reader.ReadUInt32LE();
        uint oldId = reader.ReadUInt32LE();
        uint unkFloat3 = reader.ReadUInt32LE();
        BodyType = reader.ReadUInt32LE();
        byte unkByte = reader.ReadUInt8();
        uint unkDw1 = reader.ReadUInt32LE();
        uint unkDw2 = reader.ReadUInt32LE();
        Lights[0] = reader.ReadUInt32LE();
        Lights[1] = reader.ReadUInt32LE();
        uint unkDw3 = reader.ReadUInt32LE();
        Flags[0] = (TAEFlag)reader.ReadUInt64LE();
        Flags[1] = (TAEFlag)reader.ReadUInt64LE();
        uint facing = reader.ReadUInt32LE();
        (uint startX, uint startY,
        uint endX, uint endY,
        uint offX, uint offY) = (
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE()
        );
        (uint startX2, uint startY2,
        uint endX2, uint endY2,
        uint offX2, uint offY2) = (
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE(),
            reader.ReadUInt32LE()
        );

        byte propCount = reader.ReadUInt8();
        for (int j = 0; j < propCount; ++j)
        {
            var propId = (TAEPropID)reader.ReadUInt8();
            uint propVal = reader.ReadUInt32LE();

            Props[0].Add((propId, propVal));
        }

        byte propCount2 = reader.ReadUInt8();
        for (int j = 0; j < propCount2; ++j)
        {
            var propId = (TAEPropID)reader.ReadUInt8();
            uint propVal = reader.ReadUInt32LE();

            Props[1].Add((propId, propVal));
        }

        uint stackAliasCount = reader.ReadUInt32LE();
        for (int j = 0; j < stackAliasCount; ++j)
        {
            uint amount = reader.ReadUInt32LE();
            uint amountId = reader.ReadUInt32LE();

            StackAliases.Add((amount, amountId));
        }

        uint appearanceCount = reader.ReadUInt32LE();

        for (int j = 0; j < appearanceCount; ++j)
        {
            byte subType = reader.ReadUInt8();
            if (subType == 1)
            {
                byte unk1 = reader.ReadUInt8();
                uint unk2 = reader.ReadUInt32LE();
            }
            else
            {
                uint subCount = reader.ReadUInt32LE();

                if (!Appearances.TryGetValue(subType, out Dictionary<uint, uint> dict))
                {
                    dict = [];
                    Appearances.Add(subType, dict);
                }

                for (int k = 0; k < subCount; ++k)
                {
                    uint val = reader.ReadUInt32LE();
                    uint animId = reader.ReadUInt32LE();

                    uint offset = val / 1000;
                    uint body = val % 1000;

                    if (!dict.TryAdd(body, animId + offset))
                    {

                    }
                }
            }
        }

        bool hasSitting = reader.ReadBool();
        if (hasSitting)
        {
            uint unk1 = reader.ReadUInt32LE();
            uint unk2 = reader.ReadUInt32LE();
            uint unk3 = reader.ReadUInt32LE();
            uint unk4 = reader.ReadUInt32LE();
        }

        uint radColor = reader.ReadUInt32LE();

        for (int i = 0; i < 4; ++i)
        {
            sbyte hasTexture = reader.ReadInt8();
            if (hasTexture != 0)
            {
                if (hasTexture != 1)
                {
                    // ???
                    break;
                }

                byte unk1 = reader.ReadUInt8();
                uint typeStringOffset = reader.ReadUInt32LE();
                byte textureItemsCount = reader.ReadUInt8();
                for (int j = 0; j < textureItemsCount; ++j)
                {
                    uint nameStringOff = reader.ReadUInt32LE();
                    byte unk2 = reader.ReadUInt8();
                    int unk3 = reader.ReadInt32LE();
                    int unk4 = reader.ReadInt32LE();
                    uint unk5 = reader.ReadUInt32LE();
                }

                uint unk6Count = reader.ReadUInt32LE();
                for (int j = 0; j < unk6Count; ++j)
                {
                    uint unk9 = reader.ReadUInt32LE();
                }

                uint unk10Count = reader.ReadUInt32LE();
                for (int j = 0; j < unk6Count; ++j)
                {
                    uint unk11 = reader.ReadUInt32LE();
                }
            }
        }

        byte unk12 = reader.ReadUInt8();
    }


    public uint TileId { get; }
    public uint BodyType { get; }
    public uint[] Lights { get; } = [0, 0];
    public TAEFlag[] Flags { get; } = [0, 0];
    public List<(TAEPropID PropType, uint Value)>[] Props { get; } = [[], []];
    public List<(uint, uint)> StackAliases { get; } = [];
    public Dictionary<byte, Dictionary<uint, uint>> Appearances { get; } = [];


    public bool TryGetAppearance(uint mobGraphic, out uint appearanceId)
    {
        appearanceId = 0;

        // get in account only type 0 for some unknown reason :D
        // added the Appearances.Count > 1 because seems like the conversion should happen only when there is more than 1 appearance (?)
        return Appearances.Count > 1 && Appearances.TryGetValue(0, out Dictionary<uint, uint> appearanceDict) &&
            appearanceDict.TryGetValue(mobGraphic, out appearanceId);
    }
}
