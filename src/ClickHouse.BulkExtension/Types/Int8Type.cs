﻿using System.Runtime.CompilerServices;

namespace ClickHouse.BulkExtension.Types;

class Int8Type
{
    public static readonly Int8Type Instance = new Int8Type();

    private Int8Type() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Write(Memory<byte> buffer, sbyte value)
    {
        buffer.Span[0] = (byte)value;
        return sizeof(sbyte);
    }
}