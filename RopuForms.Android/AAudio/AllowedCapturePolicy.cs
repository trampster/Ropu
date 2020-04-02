namespace RopuForms.Droid.AAudio
{
    /// <summary>
    /// Specifying if audio may or may not be captured by other apps or the system.
    ///
    /// Note that these match the equivalent values in android.media.AudioAttributes
    /// in the Android Java API.
    ///
    /// Added in API level 29.
    /// </summary>
    public enum AllowedCapturePolicy
    {
        /// <summary>
        /// Indicates that the audio may be captured by any app.
        ///
        /// For privacy, the following usages can not be recorded: Usage.VoiceCommunication,
        /// Usage.Notification, Usage.Assistance and Usage.Assistant.
        ///
        /// On Build.VERSION_CODES.Q, this means only Usage.Media
        /// and Usage.Game may be captured.
        /// 
        /// </summary>
        AllowCaptureByAll = 1,

        /// <summary>
        ///  Indicates that the audio may only be captured by system apps.
        ///
        /// System apps can capture for many purposes like accessibility, user guidance...
        /// but have strong restriction.
        /// </summary>
        AllowCaptureBySystem = 2,

        /// <summary>
        /// Indicates that the audio may not be recorded by any app, even if it is a system app.
        ///
        /// It is encouraged to use AllowCaptureBySystem instead of this value as system apps
        /// provide significant and useful features for the user (eg. accessibility).
        /// </summary>
        AllowCaptureByNone = 3
    }
}