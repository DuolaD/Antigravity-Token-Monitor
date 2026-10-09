using System;
using System.Collections.Generic;
using System.Text;

namespace AntigravityTokens.Services
{
    public enum WireType
    {
        Varint = 0,
        Fixed64 = 1,
        LengthDelimited = 2,
        StartGroup = 3,
        EndGroup = 4,
        Fixed32 = 5
    }

    public readonly ref struct ProtoField
    {
        public int FieldNumber { get; }
        public WireType WireType { get; }
        public ulong VarintValue { get; }
        public ReadOnlySpan<byte> BytesValue { get; }

        public ProtoField(int fieldNumber, ulong varintValue)
        {
            FieldNumber = fieldNumber;
            WireType = WireType.Varint;
            VarintValue = varintValue;
            BytesValue = ReadOnlySpan<byte>.Empty;
        }

        public ProtoField(int fieldNumber, WireType wireType, ReadOnlySpan<byte> bytesValue)
        {
            FieldNumber = fieldNumber;
            WireType = wireType;
            VarintValue = 0;
            BytesValue = bytesValue;
        }

        public string AsUtf8String() => Encoding.UTF8.GetString(BytesValue);
    }

    public static class ProtobufReader
    {
        public static List<(int FieldNumber, WireType WireType, ulong Varint, byte[] Data)> Parse(ReadOnlySpan<byte> buffer)
        {
            var list = new List<(int, WireType, ulong, byte[])>();
            int offset = 0;
            int length = buffer.Length;

            while (offset < length)
            {
                if (!TryReadVarint(buffer, ref offset, out ulong key))
                    break;

                int fieldNumber = (int)(key >> 3);
                WireType wireType = (WireType)(key & 0x07);

                switch (wireType)
                {
                    case WireType.Varint:
                        if (!TryReadVarint(buffer, ref offset, out ulong val))
                            return list;
                        list.Add((fieldNumber, wireType, val, Array.Empty<byte>()));
                        break;

                    case WireType.Fixed64:
                        if (offset + 8 > length)
                            return list;
                        byte[] f64 = buffer.Slice(offset, 8).ToArray();
                        offset += 8;
                        list.Add((fieldNumber, wireType, 0, f64));
                        break;

                    case WireType.LengthDelimited:
                        if (!TryReadVarint(buffer, ref offset, out ulong lenVal))
                            return list;
                        int l = (int)lenVal;
                        if (offset + l > length || l < 0)
                            return list;
                        byte[] sub = buffer.Slice(offset, l).ToArray();
                        offset += l;
                        list.Add((fieldNumber, wireType, 0, sub));
                        break;

                    case WireType.Fixed32:
                        if (offset + 4 > length)
                            return list;
                        byte[] f32 = buffer.Slice(offset, 4).ToArray();
                        offset += 4;
                        list.Add((fieldNumber, wireType, 0, f32));
                        break;

                    default:
                        return list;
                }
            }

            return list;
        }

        private static bool TryReadVarint(ReadOnlySpan<byte> buffer, ref int offset, out ulong result)
        {
            result = 0;
            int shift = 0;
            while (offset < buffer.Length)
            {
                byte b = buffer[offset++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    return true;
                shift += 7;
                if (shift >= 64)
                    return false;
            }
            return false;
        }
    }
}
