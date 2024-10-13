using System.Diagnostics.Tracing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

public class TryFormatLongBenchmark
{
    char[] _buffer = new char[1024];

    [Benchmark]
    public int TryFormatDotnet()
    {
        var sum = 0;
        1L.TryFormat(_buffer.AsSpan(), out int written1);
        sum += written1;
        12L.TryFormat(_buffer.AsSpan(), out int written2);
        sum += written2;
        123L.TryFormat(_buffer.AsSpan(), out int written3);
        sum += written3;
        1234L.TryFormat(_buffer.AsSpan(), out int written4);
        sum += written4;
        12345L.TryFormat(_buffer.AsSpan(), out int written5);
        sum += written5;
        123456L.TryFormat(_buffer.AsSpan(), out int written6);
        sum += written6;
        1234567L.TryFormat(_buffer.AsSpan(), out int written7);
        sum += written7;
        12345678L.TryFormat(_buffer.AsSpan(), out int written8);
        sum += written8;
        123456789L.TryFormat(_buffer.AsSpan(), out int written9);
        sum += written9;
        1234567890L.TryFormat(_buffer.AsSpan(), out int written10);
        sum += written10;
        12345678901L.TryFormat(_buffer.AsSpan(), out int written11);
        sum += written11;
        123456789012L.TryFormat(_buffer.AsSpan(), out int written12);
        sum += written12;
        1234567890123L.TryFormat(_buffer.AsSpan(), out int written13);
        sum += written13;
        12345678901234L.TryFormat(_buffer.AsSpan(), out int written14);
        sum += written14;
        123456789012345L.TryFormat(_buffer.AsSpan(), out int written15);
        sum += written15;
        1234567890123456L.TryFormat(_buffer.AsSpan(), out int written16);
        sum += written16;
        12345678901234567L.TryFormat(_buffer.AsSpan(), out int written17);
        sum += written17;
        123456789012345678L.TryFormat(_buffer.AsSpan(), out int written18);
        sum += written18;
        1234567890123456789L.TryFormat(_buffer.AsSpan(), out int written19);
        sum += written19;
        12345678901234567890L.TryFormat(_buffer.AsSpan(), out int written20);
        sum += written20;
        return sum;
    }

    [Benchmark]
    public int TryFormatLoop()
    {
        int written = TryFormatLoop(1, 0);
        written += TryFormatLoop(12, 0);
        written += TryFormatLoop(123, 0);
        written += TryFormatLoop(1234, 0);
        written += TryFormatLoop(12345, 0);
        written += TryFormatLoop(123456, 0);
        written += TryFormatLoop(1234567, 0);
        written += TryFormatLoop(12345678, 0);
        written += TryFormatLoop(123456789, 0);
        written += TryFormatLoop(1234567890, 0);
        written += TryFormatLoop(12345678901, 0);
        written += TryFormatLoop(123456789012, 0);
        written += TryFormatLoop(1234567890123, 0);
        written += TryFormatLoop(12345678901234, 0);
        written += TryFormatLoop(123456789012345, 0);
        written += TryFormatLoop(1234567890123456, 0);
        written += TryFormatLoop(12345678901234567, 0);
        written += TryFormatLoop(123456789012345678, 0);
        written += TryFormatLoop(1234567890123456789, 0);
        written += TryFormatLoop(12345678901234567890, 0);
        return written;
    }

    [Benchmark]
    public int TryFormatCountDigits()
    {
        int written = TryFormatCountDigits(1, 0);
        written += TryFormatCountDigits(12, 0);
        written += TryFormatCountDigits(123, 0);
        written += TryFormatCountDigits(1234, 0);
        written += TryFormatCountDigits(12345, 0);
        written += TryFormatCountDigits(123456, 0);
        written += TryFormatCountDigits(1234567, 0);
        written += TryFormatCountDigits(12345678, 0);
        written += TryFormatCountDigits(123456789, 0);
        written += TryFormatCountDigits(1234567890, 0);
        written += TryFormatCountDigits(12345678901, 0);
        written += TryFormatCountDigits(123456789012, 0);
        written += TryFormatCountDigits(1234567890123, 0);
        written += TryFormatCountDigits(12345678901234, 0);
        written += TryFormatCountDigits(123456789012345, 0);
        written += TryFormatCountDigits(1234567890123456, 0);
        written += TryFormatCountDigits(12345678901234567, 0);
        written += TryFormatCountDigits(123456789012345678, 0);
        written += TryFormatCountDigits(1234567890123456789, 0);
        written += TryFormatCountDigits(12345678901234567890, 0);
        return written;
    }

    int TryFormatLoop(ulong value, int start)
    {
        int written = 0;
        if (value == 0)
        {
            _buffer[start] = '0';
            return 1;
        }
        if (value > 0)
        {
            Span<char> reversed = stackalloc char[20];
            int reversedIndex = 0;
            while (value > 0)
            {
                var part = value % 10;
                reversed[reversedIndex] = (char)(part + 48);
                value -= value % 10;
                value = value / 10;
                written++;
                reversedIndex++;
            }
            int bufferIndex = start;
            for (int index = reversedIndex - 1; index >= 0; index--)
            {
                _buffer[start] = reversed[index];
                start++;
            }
        }
        return written;
    }


    public int TryFormatCountDigits(ulong value, int start)
    {
        var digits = CountDigits(value);
        var span = _buffer.AsSpan(start);
        ulong part = 0;
        //18446744073709551615
        switch (digits)
        {
            case 0:
                return 0;
            case 1:
                goto one;
            case 2:
                goto two;
            case 3:
                goto three;
            case 4:
                goto four;
            case 5:
                goto five;
            case 6:
                goto six;
            case 7:
                goto seven;
            case 8:
                goto eight;
            case 9:
                goto nine;
            case 10:
                goto ten;
            case 11:
                goto eleven;
            case 12:
                goto twelve;
            case 13:
                goto thirteen;
            case 14:
                goto fourteen;
            case 15:
                goto fifteen;
            case 16:
                goto sixteen;
            case 17:
                goto seventeen;
            case 18:
                goto eighteen;
            case 19:
                goto ninteen;
            case 20:
                goto twenty;
            default:
                throw new InvalidOperationException();
        }

    twenty:
        part = value % 10;
        span[19] = (char)(part + '0');
        value = (value - part) / 10;

    ninteen:
        part = value % 10;
        span[18] = (char)(part + '0');
        value = (value - part) / 10;

    eighteen:
        part = value % 10;
        span[17] = (char)(part + '0');
        value = (value - part) / 10;

    seventeen:
        part = value % 10;
        span[16] = (char)(part + '0');
        value = (value - part) / 10;

    sixteen:
        part = value % 10;
        span[15] = (char)(part + '0');
        value = (value - part) / 10;

    fifteen:
        part = value % 10;
        span[14] = (char)(part + '0');
        value = (value - part) / 10;

    fourteen:
        part = value % 10;
        span[13] = (char)(part + '0');
        value = (value - part) / 10;

    thirteen:
        part = value % 10;
        span[12] = (char)(part + '0');
        value = (value - part) / 10;

    twelve:
        part = value % 10;
        span[11] = (char)(part + '0');
        value = (value - part) / 10;

    eleven:
        part = value % 10;
        span[10] = (char)(part + '0');
        value = (value - part) / 10;

    ten:
        part = value % 10;
        span[9] = (char)(part + '0');
        value = (value - part) / 10;

    nine:
        part = value % 10;
        span[8] = (char)(part + '0');
        value = (value - part) / 10;

    eight:
        part = value % 10;
        span[7] = (char)(part + '0');
        value = (value - part) / 10;

    seven:
        part = value % 10;
        span[6] = (char)(part + '0');
        value = (value - part) / 10;

    six:
        part = value % 10;
        span[5] = (char)(part + '0');
        value = (value - part) / 10;

    five:
        part = value % 10;
        span[4] = (char)(part + '0');
        value = (value - part) / 10;

    four:
        part = value % 10;
        span[3] = (char)(part + '0');
        value = (value - part) / 10;

    three:
        part = value % 10;
        span[2] = (char)(part + '0');
        value = (value - part) / 10;

    two:
        part = value % 10;
        span[1] = (char)(part + '0');
        value = (value - part) / 10;

    one:
        part = value % 10;
        span[0] = (char)(part + '0');
        return 3;
    }

    static int CountDigits(ulong value)
    {
        // Map the log2(value) to a power of 10.
        ReadOnlySpan<byte> log2ToPow10 =
        [
            1,  1,  1,  2,  2,  2,  3,  3,  3,  4,  4,  4,  4,  5,  5,  5,
            6,  6,  6,  7,  7,  7,  7,  8,  8,  8,  9,  9,  9,  10, 10, 10,
            10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 15, 15,
            15, 16, 16, 16, 16, 17, 17, 17, 18, 18, 18, 19, 19, 19, 19, 20
        ];

        // TODO: Replace with log2ToPow10[BitOperations.Log2(value)] once https://github.com/dotnet/runtime/issues/79257 is fixed
        uint index = Unsafe.Add(ref MemoryMarshal.GetReference(log2ToPow10), BitOperations.Log2(value));

        // Read the associated power of 10.
        ReadOnlySpan<ulong> powersOf10 =
        [
            0, // unused entry to avoid needing to subtract
            0,
            10,
            100,
            1000,
            10000,
            100000,
            1000000,
            10000000,
            100000000,
            1000000000,
            10000000000,
            100000000000,
            1000000000000,
            10000000000000,
            100000000000000,
            1000000000000000,
            10000000000000000,
            100000000000000000,
            1000000000000000000,
            10000000000000000000,
        ];
        ulong powerOf10 = Unsafe.Add(ref MemoryMarshal.GetReference(powersOf10), index);

        // Return the number of digits based on the power of 10, shifted by 1
        // if it falls below the threshold.
        bool lessThan = value < powerOf10;
        return (int)(index - Unsafe.As<bool, byte>(ref lessThan)); // while arbitrary bools may be non-0/1, comparison operators are expected to return 0/1
    }
}