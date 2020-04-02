namespace RopuForms.Droid.AAudio
{
    public enum ContentType
    {

        /// <summary>
        /// Use this for spoken voice, audio books, etcetera.
        /// </summary>
        Speech = 1,

        /// <summary>
        /// Use this for pre-recorded or live music.
        /// </summary>
        Music = 2,
        
        /// <summary>
        /// Use this for a movie or video soundtrack.
        /// </summary>
        Movie = 3,

        /// <summary>
        /// Use this for sound is designed to accompany a user action,
        /// such as a click or beep sound made when the user presses a button.
        /// </summary>
        Sonification = 4
    }
}