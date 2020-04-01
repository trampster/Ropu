namespace RopuForms.Droid.AAudio
{
    public enum Format
    {
        Invalid = -1,
        Unspecified = 0,

        /// <summary>
        /// This format uses the int16_t data type.
        /// The maximum range of the data is -32768 to 32767.
        /// </summary>
        PcmI16,

        /// <summary>
        /// This format uses the float data type.
        /// The nominal range of the data is [-1.0f, 1.0f).
        /// Values outside that range may be clipped.
        /// See also 'floatData' at
        /// https://developer.android.com/reference/android/media/AudioTrack#write(float[],%20int,%20int,%20int)
        /// </summary>
        PcmFloat
    }
}