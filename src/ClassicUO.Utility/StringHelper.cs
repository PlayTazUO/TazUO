// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using SDL3;

namespace ClassicUO.Utility
{
    public static class StringHelper
    {
        private static readonly char[] _dots = { '.', ',', ';', '!' };

        /// <summary>
        /// Maps bytes 0x80..0xFF to their Windows-1251 code points. 0x98 is
        /// unassigned in Windows-1251 and is kept as the matching control
        /// character so it can be detected as invalid.
        /// </summary>
        private static readonly char[] _cp1251High =
        {
            '\u0402', '\u0403', '\u201A', '\u0453', '\u201E', '\u2026', '\u2020', '\u2021',
            '\u20AC', '\u2030', '\u0409', '\u2039', '\u040A', '\u040C', '\u040B', '\u040F',
            '\u0452', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014',
            '\u0098', '\u2122', '\u0459', '\u203A', '\u045A', '\u045C', '\u045B', '\u045F',
            '\u00A0', '\u040E', '\u045E', '\u0408', '\u00A4', '\u0490', '\u00A6', '\u00A7',
            '\u0401', '\u00A9', '\u0404', '\u00AB', '\u00AC', '\u00AD', '\u00AE', '\u0407',
            '\u00B0', '\u00B1', '\u0406', '\u0456', '\u0491', '\u00B5', '\u00B6', '\u00B7',
            '\u0451', '\u2116', '\u0454', '\u00BB', '\u0458', '\u0405', '\u0455', '\u0457',
            '\u0410', '\u0411', '\u0412', '\u0413', '\u0414', '\u0415', '\u0416', '\u0417',
            '\u0418', '\u0419', '\u041A', '\u041B', '\u041C', '\u041D', '\u041E', '\u041F',
            '\u0420', '\u0421', '\u0422', '\u0423', '\u0424', '\u0425', '\u0426', '\u0427',
            '\u0428', '\u0429', '\u042A', '\u042B', '\u042C', '\u042D', '\u042E', '\u042F',
            '\u0430', '\u0431', '\u0432', '\u0433', '\u0434', '\u0435', '\u0436', '\u0437',
            '\u0438', '\u0439', '\u043A', '\u043B', '\u043C', '\u043D', '\u043E', '\u043F',
            '\u0440', '\u0441', '\u0442', '\u0443', '\u0444', '\u0445', '\u0446', '\u0447',
            '\u0448', '\u0449', '\u044A', '\u044B', '\u044C', '\u044D', '\u044E', '\u044F',
        };

        public static IEnumerable<byte> StringToCp1252Bytes(string s, int length = -1)
        {
            length = length > 0 ? Math.Min(length, s.Length) : s.Length;

            for (int i = 0; i < length; i += char.IsSurrogatePair(s, i) ? 2 : 1)
            {
                yield return UnicodeToCp1252(char.ConvertToUtf32(s, i));
            }
        }

        public static string Cp1252ToString(ReadOnlySpan<byte> strCp1252)
        {
            var sb = new ValueStringBuilder(strCp1252.Length);

            for (int i = 0; i < strCp1252.Length; ++i)
            {
                // Every CP1252 code point maps to a single BMP char, so append it
                // directly instead of allocating a string per byte via ConvertFromUtf32.
                sb.Append((char)Cp1252ToUnicode(strCp1252[i]));
            }

            string str = sb.ToString();

            sb.Dispose();

            return str;
        }

        public static string Cp1251ToString(ReadOnlySpan<byte> strCp1251)
        {
            var sb = new ValueStringBuilder(strCp1251.Length);

            for (int i = 0; i < strCp1251.Length; ++i)
            {
                byte b = strCp1251[i];
                sb.Append(b < 0x80 ? (char)b : _cp1251High[b - 0x80]);
            }

            string str = sb.ToString();

            sb.Dispose();

            return str;
        }

        /// <summary>
        /// Decodes an entity name. The protocol declares names as Windows-1252, but
        /// some shards (typically Russian/Ukrainian) send Windows-1251 bytes instead.
        /// Both code pages map almost every byte to a character, so the wrong choice
        /// produces mojibake instead of an error. Decode with both and keep the result
        /// with the fewest invalid/implausible characters.
        /// </summary>
        public static string DecodeName(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
                return string.Empty;

            bool hasHighByte = false;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] >= 0x80)
                {
                    hasHighByte = true;

                    break;
                }
            }

            // ASCII-only names are identical in both code pages.
            if (!hasHighByte)
                return Cp1252ToString(data);

            string cp1252 = Cp1252ToString(data);
            string cp1251 = Cp1251ToString(data);

            int invalid1252 = CountInvalidNameChars(cp1252, out int latin1252, out int cyrillic1252, out int accentedLatin1252);
            int invalid1251 = CountInvalidNameChars(cp1251, out _, out int cyrillic1251, out _);

            if (invalid1251 < invalid1252)
                return cp1251;

            if (invalid1252 < invalid1251)
                return cp1252;

            // Tie: Windows-1252 is the protocol default. However, a name made mostly
            // of accented Latin letters is the signature of Cyrillic text decoded with
            // the wrong code page, so prefer Windows-1251 when it yields real Cyrillic.
            int plainLatin1252 = latin1252 - accentedLatin1252;

            if (cyrillic1251 > 0 && cyrillic1252 == 0 && accentedLatin1252 > plainLatin1252)
                return cp1251;

            return cp1252;
        }

        private static int CountInvalidNameChars(string text, out int latinLetters, out int cyrillicLetters, out int accentedLatinLetters)
        {
            int invalid = 0;
            latinLetters = 0;
            cyrillicLetters = 0;
            accentedLatinLetters = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                switch (char.GetUnicodeCategory(c))
                {
                    case UnicodeCategory.Control:
                    case UnicodeCategory.Format:
                    case UnicodeCategory.Surrogate:
                    case UnicodeCategory.OtherNotAssigned:
                        invalid++;

                        continue;
                }

                if (c == '\uFFFD')
                {
                    invalid++;

                    continue;
                }

                if (IsCyrillicLetter(c))
                {
                    cyrillicLetters++;

                    continue;
                }

                if (char.IsLetter(c))
                {
                    latinLetters++;

                    if (c >= '\u00C0' && c <= '\u00FF')
                        accentedLatinLetters++;

                    continue;
                }

                if (!char.IsDigit(c) && !char.IsWhiteSpace(c) && !char.IsPunctuation(c) && !char.IsSymbol(c))
                    invalid++;
            }

            // A name mixing Latin and Cyrillic letters is almost always mis-decoded.
            invalid += Math.Min(latinLetters, cyrillicLetters);

            return invalid;
        }

        private static bool IsCyrillicLetter(char c) =>
            (c >= '\u0400' && c <= '\u052F')
            || (c >= '\u2DE0' && c <= '\u2DFF')
            || (c >= '\uA640' && c <= '\uA69F')
            || (c >= '\u1C80' && c <= '\u1C8F');

        /// <summary>
        /// Converts a unicode code point into a cp1252 code point
        /// </summary>
        private static byte UnicodeToCp1252(int codepoint)
        {
            if (codepoint >= 0x80 && codepoint <= 0x9f)
                return (byte)'?';
            else if (codepoint <= 0xff)
                return (byte)codepoint;
            else
            {
                switch (codepoint)
                {
                    case 0x20AC: return 128; //€
                    case 0x201A: return 130; //‚
                    case 0x0192: return 131; //ƒ
                    case 0x201E: return 132; //„
                    case 0x2026: return 133; //…
                    case 0x2020: return 134; //†
                    case 0x2021: return 135; //‡
                    case 0x02C6: return 136; //ˆ
                    case 0x2030: return 137; //‰
                    case 0x0160: return 138; //Š
                    case 0x2039: return 139; //‹
                    case 0x0152: return 140; //Œ
                    case 0x017D: return 142; //Ž
                    case 0x2018: return 145; //‘
                    case 0x2019: return 146; //’
                    case 0x201C: return 147; //“
                    case 0x201D: return 148; //”
                    case 0x2022: return 149; //•
                    case 0x2013: return 150; //–
                    case 0x2014: return 151; //—
                    case 0x02DC: return 152; //˜
                    case 0x2122: return 153; //™
                    case 0x0161: return 154; //š
                    case 0x203A: return 155; //›
                    case 0x0153: return 156; //œ
                    case 0x017E: return 158; //ž
                    case 0x0178: return 159; //Ÿ
                    default: return (byte)'?';
                }
            }
        }

        /// <summary>
        /// Converts a cp1252 code point into a unicode code point
        /// </summary>
        private static int Cp1252ToUnicode(byte codepoint)
        {
            switch (codepoint)
            {
                case 128: return 0x20AC; //€
                case 130: return 0x201A; //‚
                case 131: return 0x0192; //ƒ
                case 132: return 0x201E; //„
                case 133: return 0x2026; //…
                case 134: return 0x2020; //†
                case 135: return 0x2021; //‡
                case 136: return 0x02C6; //ˆ
                case 137: return 0x2030; //‰
                case 138: return 0x0160; //Š
                case 139: return 0x2039; //‹
                case 140: return 0x0152; //Œ
                case 142: return 0x017D; //Ž
                case 145: return 0x2018; //‘
                case 146: return 0x2019; //’
                case 147: return 0x201C; //“
                case 148: return 0x201D; //”
                case 149: return 0x2022; //•
                case 150: return 0x2013; //–
                case 151: return 0x2014; //—
                case 152: return 0x02DC; //˜
                case 153: return 0x2122; //™
                case 154: return 0x0161; //š
                case 155: return 0x203A; //›
                case 156: return 0x0153; //œ
                case 158: return 0x017E; //ž
                case 159: return 0x0178; //Ÿ
                default: return codepoint;
            }
        }

        public static string CapitalizeFirstCharacter(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            if (str.Length == 1)
            {
                return char.ToUpper(str[0]).ToString();
            }

            return char.ToUpper(str[0]) + str.Substring(1);
        }


        public static string CapitalizeAllWords(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            Span<char> span = stackalloc char[str.Length];
            var sb = new ValueStringBuilder(span);
            bool capitalizeNext = true;

            for (int i = 0; i < str.Length; i++)
            {
                sb.Append(capitalizeNext ? char.ToUpper(str[i]) : str[i]);

                if (!char.IsWhiteSpace(str[i]))
                {
                    capitalizeNext = i + 1 < str.Length && char.IsWhiteSpace(str[i + 1]);
                }
            }

            string ss = sb.ToString();

            sb.Dispose();

            return ss;
        }

        public static string CapitalizeWordsByLimitator(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            Span<char> span = stackalloc char[str.Length];
            var sb = new ValueStringBuilder(span);

            bool capitalizeNext = true;

            for (int i = 0; i < str.Length; i++)
            {
                sb.Append(capitalizeNext ? char.ToUpper(str[i]) : str[i]);
                capitalizeNext = false;

                for (int j = 0; j < _dots.Length; j++)
                {
                    if (str[i] == _dots[j])
                    {
                        capitalizeNext = true;

                        break;
                    }
                }
            }

            string ss = sb.ToString();

            sb.Dispose();

            return ss;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSafeChar(int c) => c >= 0x20 && c < 0xFFFE;

        /// <summary>
        /// Removes unpaired UTF-16 surrogate characters from a string.
        /// A lone high surrogate (one not followed by a low surrogate) makes
        /// FontStashSharp read past the end of its internal buffer and throws an
        /// <see cref="IndexOutOfRangeException"/> while measuring text, so any
        /// surrogate that is not part of a valid pair must be stripped before the
        /// text reaches the renderer. Well-formed surrogate pairs are preserved.
        /// </summary>
        public static string RemoveUnpairedSurrogates(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            ReadOnlySpan<char> span = str.AsSpan();

            //Fast path: vectorized scan for the first surrogate. If there are none, keep the original reference.
            int firstSurrogate = span.IndexOfAnyInRange('\uD800', '\uDFFF');

            if (firstSurrogate < 0)
                return str;

            //Output can never be longer than the input. Stackalloc for small strings, rent for larger ones.
            char[] rented = null;
            Span<char> destination = str.Length <= 512
                ? stackalloc char[512]
                : (rented = ArrayPool<char>.Shared.Rent(str.Length));

            try
            {
                //Bulk copy the clean prefix up to the first surrogate, then process the remainder char by char.
                span.Slice(0, firstSurrogate).CopyTo(destination);
                int d = firstSurrogate;

                for (int i = firstSurrogate; i < span.Length; i++)
                {
                    char c = span[i];

                    if (char.IsHighSurrogate(c))
                    {
                        if (i + 1 < span.Length && char.IsLowSurrogate(span[i + 1]))
                        {
                            destination[d++] = c;
                            destination[d++] = span[i + 1];
                            i++;
                        }
                        //else: unpaired high surrogate -> drop it
                    }
                    else if (!char.IsLowSurrogate(c)) //Drop unpaired low surrogates, keep everything else
                    {
                        destination[d++] = c;
                    }
                }

                if (d == str.Length) //Every surrogate was part of a valid pair; nothing removed
                    return str;

                return new string(destination.Slice(0, d));
            }
            finally
            {
                if (rented != null)
                    ArrayPool<char>.Shared.Return(rented);
            }
        }

        public static void AddSpaceBeforeCapital(string[] str, bool checkAcronyms = true)
        {
            for (int i = 0; i < str.Length; i++)
            {
                str[i] = AddSpaceBeforeCapital(str[i], checkAcronyms);
            }
        }

        public static string AddSpaceBeforeCapital(string str, bool checkAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return "";
            }

            var sb = new ValueStringBuilder(str.Length * 2);
            sb.Append(str[0]);

            for (int i = 1, len = str.Length - 1; i <= len; i++)
            {
                if (char.IsUpper(str[i]))
                {
                    if (str[i - 1] != ' ' && !char.IsUpper(str[i - 1]) || checkAcronyms && char.IsUpper(str[i - 1]) && i < len && !char.IsUpper(str[i + 1]))
                    {
                        sb.Append(' ');
                    }
                }

                sb.Append(str[i]);
            }

            string s = sb.ToString();

            sb.Dispose();

            return s;
        }

        public static string RemoveUpperLowerChars(string str, bool removelower = true)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return "";
            }

            Span<char> span = stackalloc char[str.Length];
            var sb = new ValueStringBuilder(span);

            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]) == removelower || str[i] == ' ')
                {
                    sb.Append(str[i]);
                }
            }

            string ss = sb.ToString();

            sb.Dispose();

            return ss;
        }

        /// <summary>
        /// Builds a short label from a name using its capital letters (e.g. "Last Object Macro" -> "LOM").
        /// Falls back to the first letter of each whitespace/underscore/dash separated word when the
        /// name has no capitals, and to the upper-cased name itself when neither applies.
        /// </summary>
        public static string AbbreviateToInitials(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var sb = new ValueStringBuilder(stackalloc char[name.Length]);

            foreach (char c in name)
                if (char.IsUpper(c))
                    sb.Append(c);

            if (sb.Length == 0)
                foreach (string part in name.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries))
                    sb.Append(char.ToUpperInvariant(part[0]));

            string result = sb.Length > 0 ? sb.ToString() : name.ToUpperInvariant();

            sb.Dispose();

            return result;
        }

        public static string IntToAbbreviatedString(int num)
        {
            if (num > 999999)
            {
                return string.Format("{0}M+", num / 1000000);
            }

            if (num > 999)
            {
                return string.Format("{0}K+", num / 1000);
            }

            return num.ToString();
        }

        public static string GetClipboardText(bool multiline)
        {
            if (SDL.SDL_HasClipboardText() != false)
            {
                string s = multiline ? SDL.SDL_GetClipboardText() : SDL.SDL_GetClipboardText()?.Replace('\n', ' ') ?? null;

                if (!string.IsNullOrEmpty(s))
                {
                    if (s.IndexOf('\r') >= 0)
                    {
                        s = s.Replace("\r", "");
                    }

                    if (s.IndexOf('\t') >= 0)
                    {
                        return s.Replace("\t", "   ");
                    }

                    return s;
                }
            }

            return null;
        }

        public static string GetPluralAdjustedString(string str, bool plural = false)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            if (str.Contains("%"))
            {
                string[] parts = str.Split(new[] { '%' }, System.StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                {
                    return str;
                }

                Span<char> span = stackalloc char[str.Length];
                var sb = new ValueStringBuilder(span);

                sb.Append(parts[0]);

                if (parts[1].Contains("/"))
                {
                    string[] pluralparts = parts[1].Split('/');

                    if (plural)
                    {
                        sb.Append(pluralparts[0]);
                    }
                    else if (pluralparts.Length > 1)
                    {
                        sb.Append(pluralparts[1]);
                    }
                }
                else if (plural)
                {
                    sb.Append(parts[1]);
                }

                if (parts.Length == 3)
                {
                    sb.Append(parts[2]);
                }

                string ss = sb.ToString();

                sb.Dispose();

                return ss;
            }

            return str;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool UnsafeCompare(char* buffer, string str, int length)
        {
            for (int i = 0; i < length && i < str.Length; ++i)
            {
                char c0 = char.IsLetter(buffer[i]) ? char.ToLowerInvariant(buffer[i]) : buffer[i];
                char c1 = char.IsLetter(str[i]) ? char.ToLowerInvariant(str[i]) : str[i];

                if (c0 != c1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tries to parse a graphic ID from a string, supporting both decimal and hexadecimal (0x prefix) formats.
        /// </summary>
        /// <param name="text">The input string to parse</param>
        /// <param name="graphic">The parsed graphic ID</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        public static bool TryParseInt(string text, out int graphic)
        {
            graphic = 0;
            if (string.IsNullOrEmpty(text)) return false;

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(text.Substring(2), NumberStyles.AllowHexSpecifier, null, out graphic);

            return int.TryParse(text, out graphic);
        }

        /// <summary>
        /// Tries to parse a graphic ID from a string, supporting both decimal and hexadecimal (0x prefix) formats.
        /// </summary>
        /// <param name="text">The input string to parse</param>
        /// <param name="graphic">The parsed graphic ID</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        public static bool TryParseUint(string text, out uint graphic)
        {
            graphic = 0;
            if (string.IsNullOrEmpty(text)) return false;

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return uint.TryParse(text.Substring(2), NumberStyles.AllowHexSpecifier, null, out graphic);

            return uint.TryParse(text, out graphic);
        }

        public static string FormatAsCurrency(int amount) => amount.ToString("N0", CultureInfo.CurrentCulture);

        public static bool TryParseCurrency(string text, out int result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return int.TryParse(text, NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out result);
        }

        public static string Truncate(string str, int maxLength, bool addEllipsis = true)
        {
            if (string.IsNullOrEmpty(str) || maxLength <= 0)
            {
                return string.Empty;
            }

            if (str.Length <= maxLength)
            {
                return str;
            }

            if (addEllipsis)
            {
                if (maxLength <= 3)
                {
                    return str[..maxLength];
                }

                return string.Concat(str.AsSpan(0, maxLength - 3), "...");
            }

            return str[..maxLength];
        }
    }
}
