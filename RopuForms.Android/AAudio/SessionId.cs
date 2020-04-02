namespace RopuForms.Droid.AAudio
{
    public enum SessionId
    {
        /// <summary>
        /// Do not allocate a session ID.
        /// Effects cannot be used with this stream.
        /// Default.
        ///
        /// Added in API level 28.
        /// </summary>
        None = -1,
        
        /// <summary>
        /// Allocate a session ID that can be used to attach and control
        /// effects using the Java AudioEffects API.
        /// Note that using this may result in higher latency.
        ///
        /// Note that this matches the value of AudioManager.AUDIO_SESSION_ID_GENERATE.
        ///
        /// Added in API level 28.
        /// </summary>
        Allocate = 0
    }
}