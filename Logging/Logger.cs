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
    char[] _buffer = new char[1024];
    string _context = "";

    readonly LogLevel _logLevel;

    public Logger(LogLevel logLevel)
    {
        _logLevel = logLevel;
    }

    public void ForContext(string context)
    {
        _context = context;
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
        formatted.CopyTo(_buffer.AsSpan(index));
        index += formatted.Length;
        Console.Out.WriteLine(_buffer.AsSpan(0, index));
    }

    void Log(string value, string level)
    {
        var index = WriteContext(level);
        var formatted = value;
        formatted.CopyTo(_buffer.AsSpan(index));
        index += formatted.Length;
        Console.Out.WriteLine(_buffer.AsSpan(0, index));
    }

    void Log(long value, string level)
    {
        var index = WriteContext(level);
        int written = Write(value, index);
        Console.Out.WriteLine(_buffer.AsSpan(0, written));
    }

    int WriteContext(string level)
    {
        int index = 0;
        _buffer[index] = '[';
        index++;

        DateTime.UtcNow.TryFormat(_buffer.AsSpan(index), out int written, "O");
        index += written;

        _buffer[index] = ' ';
        index++;

        level.CopyTo(_buffer.AsSpan(index));

        index += level.Length;

        _buffer[index] = ' ';
        index++;

        _context.CopyTo(_buffer.AsSpan(index));
        index += _context.Length;

        _buffer[index] = ']';
        index++;

        _buffer[index] = ' ';
        index++;

        return index;
    }

    int Write(long value, int start)
    {
        int written = 0;
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
}