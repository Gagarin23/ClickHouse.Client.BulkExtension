﻿using System.Runtime.CompilerServices;

namespace ClickHouse.BulkExtension.Types;

class BooleanType
{
    public static readonly BooleanType Instance = new BooleanType();

    private BooleanType() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Write(Memory<byte> buffer, bool value)
    {
        buffer.Span[0] = value ? (byte)1 : (byte)0;
        return sizeof(byte);
    }
}