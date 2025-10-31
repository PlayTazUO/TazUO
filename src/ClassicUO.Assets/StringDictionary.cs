// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.Text;
using ClassicUO.IO;
using ClassicUO.Utility;

namespace ClassicUO.Assets;

// https://github.com/cbnolok/UOETE/blob/master/src/uostringdictionary.cpp
public sealed class StringDictionaryLoader : UOFileLoader
{
    private string[] _strings = Array.Empty<string>();

    public StringDictionaryLoader(UOFileManager fileManager) : base(fileManager)
    {
    }

    public bool TryGetString(int index, out string str)
    {
        if (index < 0 || index >= _strings.Length)
        {
            str = string.Empty;
            return false;
        }

        str = _strings[index];
        return true;
    }

    public override void Load()
    {
        string path = FileManager.GetUOFilePath("string_dictionary.uop");
        if (!File.Exists(path))
            return;

        using var file = new UOFileUop(path, "build/stringdictionary/string_dictionary.bin");
        file.FillEntries();

        ref readonly UOFileIndex index = ref file.GetValidRefEntry(0);
        if (index.Length == 0)
            return;

        file.Seek(index.Offset, SeekOrigin.Begin);
        byte[] buf = new byte[file.Length];
        file.Read(buf);

        byte[] dbuf = new byte[index.DecompressedLength];
        ZLib.ZLibError result = ZLib.Decompress(buf, dbuf);
        if (result != ZLib.ZLibError.Ok)
            return;

        var reader = new StackDataReader(dbuf);

        ulong unk1 = reader.ReadUInt64LE();
        uint count = reader.ReadUInt32LE();
        _strings = new string[count];
        uint unk2 = reader.ReadUInt32LE();
        for (int i = 0; i < count; ++i)
        {
            ushort len = reader.ReadUInt16LE();
            string str = Encoding.UTF8.GetString(reader.Buffer.Slice(reader.Position, len));
            _strings[i] = str;
            reader.Skip(len);
        }
    }
}