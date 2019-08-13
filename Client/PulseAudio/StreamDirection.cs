
namespace Ropu.Client.PulseAudio
{
    public enum StreamDirection 
    {
        NoDirection,   /**< Invalid direction */
        Playback,      /**< Playback stream */
        Record,        /**< Record stream */
        Upload         /**< Sample upload stream */
    }
}