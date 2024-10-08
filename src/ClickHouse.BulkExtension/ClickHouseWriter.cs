using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using ClickHouse.BulkExtension.Types;

namespace ClickHouse.BulkExtension;

class ClickHouseWriter : IAsyncDisposable
{
    private const int BufferThreshold = 64;

    public static readonly MethodInfo WriteMethod = typeof(ClickHouseWriter).GetMethod(nameof(WriteAsync), BindingFlags.Public | BindingFlags.Instance)!;
    public static readonly MethodInfo StringWriteMethod = typeof(ClickHouseWriter).GetMethod(nameof(WriteStringAsync), BindingFlags.Public | BindingFlags.Instance)!;

    private readonly Stream _underlyingStream;
    private IMemoryOwner<byte> _memoryOwner;
    private int _position;

    public ClickHouseWriter(Stream underlyingStream, int bufferSize)
    {
        _underlyingStream = underlyingStream;
        _memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
    }

    // Try to inline methods [type_name]Type.Write => WriteAsync => main foreach cycle.
    // In theory, we will get a single allocated stack frame inside _writeFunction(writer, _source) method,
    // except UuidType.Write, because it uses a stackalloc function.
    // As a result, in benchmarks we have a 3-7% cpu-bound performance improvement.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask WriteStringAsync(string value)
    {
        var required = value.Length * 3;
        var bufferLength = _memoryOwner.Memory.Length;
        if (required >= bufferLength - _position)
        {
            await FlushAsync();
            Resize(required + bufferLength);
        }

        var bytesWritten = StringType.Instance.Write(_memoryOwner.Memory[_position..], value);
        _position += bytesWritten;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask WriteAsync<T>(Func<Memory<byte>, T, int> writeFunction, T value)
    {
        var memory = _memoryOwner.Memory;
        if (memory.Length - _position <= BufferThreshold)
        {
            await FlushAsync();
        }

        var written = writeFunction(memory[_position..], value);
        _position += written;
    }

    public async ValueTask DisposeAsync()
    {
        await FlushAsync();
        _memoryOwner.Dispose();
    }

    private async Task FlushAsync()
    {
        if (_position == 0)
        {
            return;
        }
        await _underlyingStream.WriteAsync(_memoryOwner.Memory[.._position]);
        _position = 0;
    }

    private void Resize(int required)
    {
        _memoryOwner.Dispose();
        _memoryOwner = MemoryPool<byte>.Shared.Rent(required);
        _position = 0;
    }
}