namespace RopuForms.Droid.AAudio
{
    public enum SharingMode
    {
        /// <summary>
        /// This will be the only stream using a particular source or sink.
        /// This mode will provide the lowest possible latency.
        /// You should close Exclusive streams immediately when you are not using them.
        /// </summary>
        Exclusive,
        /// <summary>
        /// Multiple applications will be mixed by the AAudio Server.
        /// This will have higher latency than the Exclusive mode.
        /// </summary>
        Shared,
    }
}