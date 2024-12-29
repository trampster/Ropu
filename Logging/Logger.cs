using System.Runtime.InteropServices.Marshalling;

namespace Ropu.Logging;


public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public class Logger : ILogger
{
    [ThreadStatic]
    static char[]? _buffer;
    string _context = "";

    readonly LogLevel _logLevel;

    public Logger(LogLevel logLevel) : this(logLevel, "")
    {
    }

    public Logger(LogLevel logLevel, string context)
    {
        _logLevel = logLevel;
        _context = context;
    }

    static char[] Buffer
    {
        get
        {
            if (_buffer == null)
            {
                _buffer = new char[1024];
            }
            return _buffer;
        }
    }

    public ILogger ForContext(string context)
    {
        return new Logger(_logLevel, context);
    }

    public void Debug(long value)
    {
        if (_logLevel > LogLevel.Debug)
        {
            return;
        }
        Log(value, "Debug");
    }

    public void Debug(string value)
    {
        if (_logLevel > LogLevel.Debug)
        {
            return;
        }
        Log(value, "Debug");
    }

    public void Warning(string value)
    {
        if (_logLevel > LogLevel.Warning)
        {
            return;
        }
        Log(value, "Warn");
    }

    public void Information(string value)
    {
        if (_logLevel > LogLevel.Info)
        {
            return;
        }
        Log(value, "Info");
    }

    public void Debug(ZeroAllocationInterpolationHandler handler)
    {
        if (_logLevel > LogLevel.Debug)
        {
            return;
        }
        Log(handler, "Debug");
    }

    public void Warning(ZeroAllocationInterpolationHandler handler)
    {
        if (_logLevel > LogLevel.Warning)
        {
            return;
        }
        Log(handler, "Warn");
    }

    public void Information(ZeroAllocationInterpolationHandler handler)
    {
        if (_logLevel > LogLevel.Info)
        {
            return;
        }
        Log(handler, "Info");
    }

    void Log(ZeroAllocationInterpolationHandler handler, string level)
    {
        var index = WriteContext(level);
        var formatted = handler.GetFormattedText();
        var buffer = Buffer;
        formatted.CopyTo(buffer.AsSpan(index));
        index += formatted.Length;
        Console.Out.WriteLine(buffer.AsSpan(0, index));
    }

    void Log(string value, string level)
    {
        var index = WriteContext(level);
        var formatted = value;
        var buffer = Buffer;
        formatted.CopyTo(buffer.AsSpan(index));
        index += formatted.Length;
        Console.Out.WriteLine(buffer.AsSpan(0, index));
    }

    void Log(long value, string level)
    {
        var index = WriteContext(level);
        int written = Write(value, index);
        Console.Out.WriteLine(Buffer.AsSpan(0, written));
    }

    int WriteContext(string level)
    {
        int index = 0;
        var buffer = Buffer;

        buffer[index] = '[';
        index++;

        DateTime.UtcNow.TryFormat(buffer.AsSpan(index), out int written, "O");
        index += written;

        buffer[index] = ' ';
        index++;

        level.CopyTo(buffer.AsSpan(index));

        index += level.Length;

        buffer[index] = ' ';
        index++;

        buffer[index] = '[';
        index++;
        _context.CopyTo(buffer.AsSpan(index));
        index += _context.Length;

        buffer[index] = ']';
        index++;

        buffer[index] = ' ';
        index++;

        return index;
    }

    int Write(long value, int start)
    {
        int written = 0;
        var buffer = Buffer;

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
                buffer[start] = reversed[index];
                start++;
            }
        }
        return written;
    }
}