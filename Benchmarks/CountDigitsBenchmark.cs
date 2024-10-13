using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

public class CountDigitsBenchmark
{
    [Benchmark]
    public int CountDigitsDotnet()
    {
        int sum = CountDigitsDotnet(1);
        sum += CountDigitsDotnet(12);
        sum += CountDigitsDotnet(123);
        sum += CountDigitsDotnet(1234);
        sum += CountDigitsDotnet(12345);
        sum += CountDigitsDotnet(123456);
        sum += CountDigitsDotnet(1234567);
        sum += CountDigitsDotnet(12345678);
        sum += CountDigitsDotnet(123456789);
        sum += CountDigitsDotnet(1234567890);
        sum += CountDigitsDotnet(12345678901);
        sum += CountDigitsDotnet(123456789012);
        sum += CountDigitsDotnet(1234567890123);
        sum += CountDigitsDotnet(12345678901234);
        sum += CountDigitsDotnet(123456789012345);
        sum += CountDigitsDotnet(1234567890123456);
        sum += CountDigitsDotnet(12345678901234567);
        sum += CountDigitsDotnet(123456789012345678);
        sum += CountDigitsDotnet(1234567890123456789);
        sum += CountDigitsDotnet(12345678901234567890);
        return sum;
    }

    static readonly ImmutableArray<byte> _log2ToPow10 =
        [
            1,  1,  1,  2,  2,  2,  3,  3,  3,  4,  4,  4,  4,  5,  5,  5,
            6,  6,  6,  7,  7,  7,  7,  8,  8,  8,  9,  9,  9,  10, 10, 10,
            10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 15, 15,
            15, 16, 16, 16, 16, 17, 17, 17, 18, 18, 18, 19, 19, 19, 19, 20
        ];

    static readonly ImmutableArray<ulong> _powersOf10 =
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

    [Benchmark]
    public int CountDigitsStaticReadonly()
    {
        int sum = CountDigitsStaticReadonly(1);
        sum += CountDigitsStaticReadonly(12);
        sum += CountDigitsStaticReadonly(123);
        sum += CountDigitsStaticReadonly(1234);
        sum += CountDigitsStaticReadonly(12345);
        sum += CountDigitsStaticReadonly(123456);
        sum += CountDigitsStaticReadonly(1234567);
        sum += CountDigitsStaticReadonly(12345678);
        sum += CountDigitsStaticReadonly(123456789);
        sum += CountDigitsStaticReadonly(1234567890);
        sum += CountDigitsStaticReadonly(12345678901);
        sum += CountDigitsStaticReadonly(123456789012);
        sum += CountDigitsStaticReadonly(1234567890123);
        sum += CountDigitsStaticReadonly(12345678901234);
        sum += CountDigitsStaticReadonly(123456789012345);
        sum += CountDigitsStaticReadonly(1234567890123456);
        sum += CountDigitsStaticReadonly(12345678901234567);
        sum += CountDigitsStaticReadonly(123456789012345678);
        sum += CountDigitsStaticReadonly(1234567890123456789);
        sum += CountDigitsStaticReadonly(12345678901234567890);
        return sum;
    }

    static int CountDigitsStaticReadonly(ulong value)
    {
        // Map the log2(value) to a power of 10.
        ReadOnlySpan<byte> log2ToPow10 = _log2ToPow10.AsSpan();

        // TODO: Replace with log2ToPow10[BitOperations.Log2(value)] once https://github.com/dotnet/runtime/issues/79257 is fixed
        uint index = Unsafe.Add(ref MemoryMarshal.GetReference(log2ToPow10), BitOperations.Log2(value));

        // Read the associated power of 10.
        ReadOnlySpan<ulong> powersOf10 = _powersOf10.AsSpan();
        ulong powerOf10 = Unsafe.Add(ref MemoryMarshal.GetReference(powersOf10), index);

        // Return the number of digits based on the power of 10, shifted by 1
        // if it falls below the threshold.
        bool lessThan = value < powerOf10;
        return (int)(index - Unsafe.As<bool, byte>(ref lessThan)); // while arbitrary bools may be non-0/1, comparison operators are expected to return 0/1
    }

    static int CountDigitsDotnet(ulong value)
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