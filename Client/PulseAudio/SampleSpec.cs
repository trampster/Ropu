
namespace Ropu.Client.PulseAudio
{
    public struct SampleSpec 
    {
        /**< The sample format */
        public SampleFormat format;

        /**< The sample rate. (e.g. 44100) */
        public uint rate;
        /**< Audio channels. (1 for mono, 2 for stereo, ...) */
        public byte channels;
    }
}