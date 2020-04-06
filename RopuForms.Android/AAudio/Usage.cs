namespace RopuForms.Droid.AAudio
{
    public enum Usage
    {

        /// <summary>
        /// Use this for streaming media, music performance, video, podcasts, etcetera.
        /// </summary>
        Media = 1,

        /// <summary>
        /// Use this for voice over IP, telephony, etcetera.
        /// </summary>
        VoiceCommunication = 2,

        /// <summary>
        /// Use this for sounds associated with telephony such as busy tones, DTMF, etcetera.
        /// </summary>
        VoiceCommunicationSignalling = 3,

        /// <summary>
        /// Use this to demand the users attention.
        /// </summary>
        Alarm = 4,

        /// <summary>
        /// Use this for notifying the user when a message has arrived or some
        /// other background event has occured.
        /// </summary>
        UsageNotification = 5,

        /// <summary>
        /// Use this when the phone rings.
        /// </summary>
        NotificationRingtone = 6,

        /// <summary>
        /// Use this to attract the users attention when, for example, the battery is low.
        /// </summary>
        NotificationEvent = 10,

        /// <summary>
        /// Use this for screen readers, etcetera.
        /// </summary>
        AssistanceAccessibility = 11,
        
        /// <summary>
        /// Use this for driving or navigation directions.
        /// </summary>
        AssistanceNavigationGuidance = 12,

        /// <summary>
        /// Use this for user interface sounds, beeps, etcetera.
        /// </summary>
        AssistanceSonification = 13,

        /// <summary>
        /// Use this for game audio and sound effects.
        /// </summary>
        UsageGame = 14,

        /// <summary>
        /// Use this for audio responses to user queries, audio instructions or help utterances.
        /// </summary>
        UsageAssistant = 16
    }
}