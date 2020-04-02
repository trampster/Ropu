namespace RopuForms.Droid.AAudio
{
    public enum InputPreset
    {
        /// <summary>
        /// Use this preset when other presets do not apply.
        /// </summary>
        Generic = 1,

        /// <summary>
        /// Use this preset when recording video.
        /// </summary>
        Camcorder = 5,

        /// <summary>
        /// Use this preset when doing speech recognition.
        /// </summary>
        VoiceRecognition = 6,

        /// <summary>
        /// Use this preset when doing telephony or voice messaging.
        /// </summary>
        VoiceCommunication = 7,

        /// <summary>
        /// Use this preset to obtain an input with no effects.
        /// Note that this input will not have automatic gain control
        /// so the recorded volume may be very low.
        /// </summary>
        Unprocessed = 9,

        /// <summary>
        /// Use this preset for capturing audio meant to be processed in real time
        /// and played back for live performance (e.g karaoke).
        /// The capture path will minimize latency and coupling with playback path.
        /// </summary>
        VoicePerformance = 10,
    }
}