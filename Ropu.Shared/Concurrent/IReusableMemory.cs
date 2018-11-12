using System;

namespace Ropu.Shared.Concurrent
{
    public interface IReusableMemory<T> 
    {
        Span<T> AsSpan();

        int Length {get;}

        T[] Memory {get;}
    }
}