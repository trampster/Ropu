
namespace Ropu.Client.PulseAudio
{
    public enum SampleFormat 
    {
        PA_SAMPLE_U8,
        /**< Unsigned 8 Bit PCM */

        PA_SAMPLE_ALAW,
        /**< 8 Bit a-Law */

        PA_SAMPLE_ULAW,
        /**< 8 Bit mu-Law */

        PA_SAMPLE_S16LE,
        /**< Signed 16 Bit PCM, little endian (PC) */

        PA_SAMPLE_S16BE,
        /**< Signed 16 Bit PCM, big endian */

        PA_SAMPLE_FLOAT32LE,
        /**< 32 Bit IEEE floating point, little endian (PC), range -1.0 to 1.0 */

        PA_SAMPLE_FLOAT32BE,
        /**< 32 Bit IEEE floating point, big endian, range -1.0 to 1.0 */

        PA_SAMPLE_S32LE,
        /**< Signed 32 Bit PCM, little endian (PC) */

        PA_SAMPLE_S32BE,
        /**< Signed 32 Bit PCM, big endian */

        PA_SAMPLE_S24LE,
        /**< Signed 24 Bit PCM packed, little endian (PC). \since 0.9.15 */

        PA_SAMPLE_S24BE,
        /**< Signed 24 Bit PCM packed, big endian. \since 0.9.15 */

        PA_SAMPLE_S24_32LE,
        /**< Signed 24 Bit PCM in LSB of 32 Bit words, little endian (PC). \since 0.9.15 */

        PA_SAMPLE_S24_32BE,
        /**< Signed 24 Bit PCM in LSB of 32 Bit words, big endian. \since 0.9.15 */

        PA_SAMPLE_MAX,
        /**< Upper limit of valid sample types */

        PA_SAMPLE_INVALID = -1
        /**< An invalid value */
    };
}