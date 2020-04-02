namespace RopuForms.Droid.AAudio
{
    public enum PerformanceMode
    {

        /// <summary>
        /// No particular performance needs. Default.
        /// </summary>
        None = 10,

        /// <summary>
        /// Extending battery life is more important than low latency.
        /// This mode is not supported in input streams.
        /// For input, mode NONE will be used if this is requested.
        /// </summary>
        PowerSaving,

        /// <summary>
        /// Reducing latency is more important than battery life.
        /// </summary>
        LowLatency,
    }
}