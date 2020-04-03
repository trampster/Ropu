namespace RopuForms.Droid.AAudio
{
    public enum StreamState
    {
        Uninitialized = 0,
        Unknown,
        Open,
        Starting,
        Started,
        Pausing,
        Paused,
        Flushing,
        Flushed,
        Stopping,
        Stopped,
        Closing,
        Closed,
        Disconnected
    }
}