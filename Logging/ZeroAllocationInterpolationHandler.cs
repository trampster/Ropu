using System.Net;
using System.Runtime.CompilerServices;
using Ropu.BalancerProtocol;

namespace Ropu.Logging;

[InterpolatedStringHandler]
public ref struct ZeroAllocationInterpolationHandler
{
    [ThreadStatic] static char[] _buffer = new char[1024];

    int _currentIndex = 0;

    public ZeroAllocationInterpolationHandler(int literalLength, int formattedCount)
    {
        if (_buffer == null)
        {
            _buffer = new char[1024];
        }
    }

    public void AppendLiteral(string s)
    {
        s.AsSpan().CopyTo(_buffer.AsSpan(_currentIndex));
        _currentIndex += s.Length;
    }

    public void AppendFormatted(string formatted)
    {
        int written = Write(formatted, _currentIndex);
        _currentIndex += written;
    }

    public void AppendFormatted(int formatted)
    {
        int written = Write(formatted, _currentIndex);
        _currentIndex += written;
    }

    public void AppendFormatted(byte formatted)
    {
        int written = Write(formatted, _currentIndex);
        _currentIndex += written;
    }

    public void AppendFormatted(long formatted)
    {
        int written = Write(formatted, _currentIndex);
        _currentIndex += written;
    }

    public void AppendFormatted(ushort formatted)
    {
        int written = Write(formatted, _currentIndex);
        _currentIndex += written;
    }

    public void AppendFormatted(BalancerPacketTypes formatted)
    {
        string packetType = "";
        switch (formatted)
        {
            case BalancerPacketTypes.DistributorHeartbeat:
                packetType = nameof(BalancerPacketTypes.DistributorHeartbeat);
                break;
            case BalancerPacketTypes.HeartbeatResponse:
                packetType = nameof(BalancerPacketTypes.HeartbeatResponse);
                break;
            case BalancerPacketTypes.RegisterDistributor:
                packetType = nameof(BalancerPacketTypes.RegisterDistributor);
                break;
            case BalancerPacketTypes.RegisterDistributorResponse:
                packetType = nameof(BalancerPacketTypes.RegisterDistributorResponse);
                break;
            case BalancerPacketTypes.RegisterRouter:
                packetType = nameof(BalancerPacketTypes.RegisterRouter);
                break;
            case BalancerPacketTypes.RegisterRouterResponse:
                packetType = nameof(BalancerPacketTypes.RegisterRouterResponse);
                break;
            case BalancerPacketTypes.ResolveUnit:
                packetType = nameof(BalancerPacketTypes.ResolveUnit);
                break;
            case BalancerPacketTypes.ResolveUnitResponse:
                packetType = nameof(BalancerPacketTypes.ResolveUnitResponse);
                break;
            case BalancerPacketTypes.RouterAssignment:
                packetType = nameof(BalancerPacketTypes.RouterAssignment);
                break;
            case BalancerPacketTypes.RouterAssignmentRequest:
                packetType = nameof(BalancerPacketTypes.RouterAssignmentRequest);
                break;
            case BalancerPacketTypes.RouterHeartbeat:
                packetType = nameof(BalancerPacketTypes.RouterHeartbeat);
                break;
            default:
                throw new InvalidOperationException($"Unknown BalancerPacketType {formatted}");
        }
        int written = Write(packetType, _currentIndex);
        _currentIndex += written;
    }

    public void AppendFormatted(SocketAddress address)
    {
        var addressBytes = address.Buffer.Span.Slice(4, 4);
        _currentIndex += Write(addressBytes[0], _currentIndex);

        _buffer[_currentIndex] = '.';
        _currentIndex++;

        _currentIndex += Write(addressBytes[1], _currentIndex);

        _buffer[_currentIndex] = '.';
        _currentIndex++;

        _currentIndex += Write(addressBytes[2], _currentIndex);

        _buffer[_currentIndex] = '.';
        _currentIndex++;

        _currentIndex += Write(addressBytes[3], _currentIndex);
        _buffer[_currentIndex] = ':';
        _currentIndex++;

        var port = (ushort)((address[2] << 8) + address[3]);

        _currentIndex += Write(port, _currentIndex);
    }

    int Write(string value, int start)
    {
        value.CopyTo(_buffer.AsSpan(start));
        return value.Length;
    }

    int Write(long value, int start)
    {
        int written = 0;
        if (value == 0)
        {
            _buffer[start] = '0';
            return 1;
        }
        if (value > 0)
        {
            Span<char> reversed = stackalloc char[19];
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

    public Span<char> GetFormattedText()
    {
        return _buffer.AsSpan(0, _currentIndex);
    }
}
