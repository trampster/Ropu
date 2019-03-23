using System;

namespace Ropu.Client
{
    public interface IBeepPlayer : IDisposable
    {
        void PlayGoAhead();
        void PlayDenied();
    }
}