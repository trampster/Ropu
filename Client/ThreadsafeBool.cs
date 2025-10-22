namespace Ropu.Client;

public class ThreadSafeBool
{
    int _value = 0;

    public ThreadSafeBool()
    {

    }

    public bool Value
    {
        get
        {
            return Interlocked.CompareExchange(ref _value, _value, _value) == 1;
        }
        set
        {
            Interlocked.Exchange(ref _value, value ? 1 : 0);
        }
    }
}