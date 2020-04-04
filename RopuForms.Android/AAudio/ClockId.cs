namespace RopuForms.Droid.AAudio
{
    public enum ClockId
    {
        /// <summary>
        /// Identifier for system-wide realtime clock.
        /// </summary>
        Realtime = 0,
        /// <summary>
        /// High-resolution timer from the CPU.
        /// </summary>
        Monotonic =	1,
        /// <summary>
        /// High-resolution timer from the CPU.
        /// </summary>
        ProcessCpuTimeId = 2,
        /// <summary>
        /// Thread-specific CPU-time clock. 
        /// </summary>
        ThreadCpuTimeId = 3,
        /// <summary>
        /// Monotonic system-wide clock, not adjusted for frequency scaling. 
        /// </summary>
        MonotonicRaw = 4,
        /// <summary>
        /// Identifier for system-wide realtime clock, updated only on ticks.
        /// </summary>
        RealtimeCourse = 5,
        /// <summary>
        /// Monotonic system-wide clock, updated only on ticks.
        /// </summary>
        MonotonicCoarse = 6,
        /// <summary>
        /// Monotonic system-wide clock that includes time spent in suspension.
        /// </summary>
        Boottime = 7,
        /// <summary>
        /// Like CLOCK_REALTIME but also wakes suspended system.
        /// </summary>
        RealtimeAlarm = 8,
        /// <summary>
        /// Like CLOCK_BOOTTIME but also wakes suspended system.
        /// </summary>
        BoottimeAlarm = 9,
        /// <summary>
        /// Like CLOCK_REALTIME but in International Atomic Time.
        /// </summary>
        TAI = 11
    }
}