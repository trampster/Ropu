using System;
using NUnit.Framework;

namespace Ropu.Tests
{
    public static class SpanAssert
    {
        public static void AreEqual<T>(Span<T> expected, Span<T> actual)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length), $"Spans Length didn't match, expected length {expected.Length} but was {actual.Length}");
            for(int index = 0; index < expected.Length; index++)
            {
                Assert.That(expected[index], Is.EqualTo(actual[index]), $"Spans didn't match at index {index}");
            }
        }
    }
}