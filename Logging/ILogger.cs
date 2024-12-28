namespace Ropu.Logging;

public interface ILogger
{
    ILogger ForContext(string context);

    void Debug(long value);

    void Debug(string value);

    void Debug(ZeroAllocationInterpolationHandler handler);

    void Warning(string value);

    void Warning(ZeroAllocationInterpolationHandler handler);

    void Information(string value);

    void Information(ZeroAllocationInterpolationHandler handler);
}