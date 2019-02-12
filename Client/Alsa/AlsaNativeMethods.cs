using System;
using System.Runtime.InteropServices;

namespace Ropu.Client.Alsa
{
    public enum snd_pcm_stream_t
    {
            SND_PCM_STREAM_PLAYBACK = 0, 
            SND_PCM_STREAM_CAPTURE = 1
    }

    public enum snd_pcm_access_t 
    {
        /** mmap access with simple interleaved channels */
        SND_PCM_ACCESS_MMAP_INTERLEAVED = 0,
        /** mmap access with simple non interleaved channels */
        SND_PCM_ACCESS_MMAP_NONINTERLEAVED = 1,
        /** mmap access with complex placement */
        SND_PCM_ACCESS_MMAP_COMPLEX = 2,
        /** snd_pcm_readi/snd_pcm_writei access */
        SND_PCM_ACCESS_RW_INTERLEAVED = 3,
        /** snd_pcm_readn/snd_pcm_writen access */
        SND_PCM_ACCESS_RW_NONINTERLEAVED = 4,
    }

    public enum snd_pcm_format_t 
    {
        /** Unknown */
        SND_PCM_FORMAT_UNKNOWN = -1,
        /** Signed 8 bit */
        SND_PCM_FORMAT_S8 = 0,
        /** Unsigned 8 bit */
        SND_PCM_FORMAT_U8,
        /** Signed 16 bit Little Endian */
        SND_PCM_FORMAT_S16_LE,
        /** Signed 16 bit Big Endian */
        SND_PCM_FORMAT_S16_BE,
        /** Unsigned 16 bit Little Endian */
        SND_PCM_FORMAT_U16_LE,
        /** Unsigned 16 bit Big Endian */
        SND_PCM_FORMAT_U16_BE,
        /** Signed 24 bit Little Endian using low three bytes in 32-bit word */
        SND_PCM_FORMAT_S24_LE,
        /** Signed 24 bit Big Endian using low three bytes in 32-bit word */
        SND_PCM_FORMAT_S24_BE,
        /** Unsigned 24 bit Little Endian using low three bytes in 32-bit word */
        SND_PCM_FORMAT_U24_LE,
        /** Unsigned 24 bit Big Endian using low three bytes in 32-bit word */
        SND_PCM_FORMAT_U24_BE,
        /** Signed 32 bit Little Endian */
        SND_PCM_FORMAT_S32_LE,
        /** Signed 32 bit Big Endian */
        SND_PCM_FORMAT_S32_BE,
        /** Unsigned 32 bit Little Endian */
        SND_PCM_FORMAT_U32_LE,
        /** Unsigned 32 bit Big Endian */
        SND_PCM_FORMAT_U32_BE,
        /** Float 32 bit Little Endian, Range -1.0 to 1.0 */
        SND_PCM_FORMAT_FLOAT_LE,
        /** Float 32 bit Big Endian, Range -1.0 to 1.0 */
        SND_PCM_FORMAT_FLOAT_BE,
        /** Float 64 bit Little Endian, Range -1.0 to 1.0 */
        SND_PCM_FORMAT_FLOAT64_LE,
        /** Float 64 bit Big Endian, Range -1.0 to 1.0 */
        SND_PCM_FORMAT_FLOAT64_BE,
        /** IEC-958 Little Endian */
        SND_PCM_FORMAT_IEC958_SUBFRAME_LE,
        /** IEC-958 Big Endian */
        SND_PCM_FORMAT_IEC958_SUBFRAME_BE,
        /** Mu-Law */
        SND_PCM_FORMAT_MU_LAW,
        /** A-Law */
        SND_PCM_FORMAT_A_LAW,
        /** Ima-ADPCM */
        SND_PCM_FORMAT_IMA_ADPCM,
        /** MPEG */
        SND_PCM_FORMAT_MPEG,
        /** GSM */
        SND_PCM_FORMAT_GSM,
        /** Special */
        SND_PCM_FORMAT_SPECIAL = 31,
        /** Signed 24bit Little Endian in 3bytes format */
        SND_PCM_FORMAT_S24_3LE = 32,
        /** Signed 24bit Big Endian in 3bytes format */
        SND_PCM_FORMAT_S24_3BE,
        /** Unsigned 24bit Little Endian in 3bytes format */
        SND_PCM_FORMAT_U24_3LE,
        /** Unsigned 24bit Big Endian in 3bytes format */
        SND_PCM_FORMAT_U24_3BE,
        /** Signed 20bit Little Endian in 3bytes format */
        SND_PCM_FORMAT_S20_3LE,
        /** Signed 20bit Big Endian in 3bytes format */
        SND_PCM_FORMAT_S20_3BE,
        /** Unsigned 20bit Little Endian in 3bytes format */
        SND_PCM_FORMAT_U20_3LE,
        /** Unsigned 20bit Big Endian in 3bytes format */
        SND_PCM_FORMAT_U20_3BE,
        /** Signed 18bit Little Endian in 3bytes format */
        SND_PCM_FORMAT_S18_3LE,
        /** Signed 18bit Big Endian in 3bytes format */
        SND_PCM_FORMAT_S18_3BE,
        /** Unsigned 18bit Little Endian in 3bytes format */
        SND_PCM_FORMAT_U18_3LE,
        /** Unsigned 18bit Big Endian in 3bytes format */
        SND_PCM_FORMAT_U18_3BE,
        /* G.723 (ADPCM) 24 kbit/s, 8 samples in 3 bytes */
        SND_PCM_FORMAT_G723_24,
        /* G.723 (ADPCM) 24 kbit/s, 1 sample in 1 byte */
        SND_PCM_FORMAT_G723_24_1B,
        /* G.723 (ADPCM) 40 kbit/s, 8 samples in 3 bytes */
        SND_PCM_FORMAT_G723_40,
        /* G.723 (ADPCM) 40 kbit/s, 1 sample in 1 byte */
        SND_PCM_FORMAT_G723_40_1B,
        /* Direct Stream Digital (DSD) in 1-byte samples (x8) */
        SND_PCM_FORMAT_DSD_U8,
        /* Direct Stream Digital (DSD) in 2-byte samples (x16) */
        SND_PCM_FORMAT_DSD_U16_LE,
        /* Direct Stream Digital (DSD) in 4-byte samples (x32) */
        SND_PCM_FORMAT_DSD_U32_LE,
        /* Direct Stream Digital (DSD) in 2-byte samples (x16) */
        SND_PCM_FORMAT_DSD_U16_BE,
        /* Direct Stream Digital (DSD) in 4-byte samples (x32) */
        SND_PCM_FORMAT_DSD_U32_BE,
        SND_PCM_FORMAT_LAST = SND_PCM_FORMAT_DSD_U32_BE,
        /** Signed 16 bit CPU endian */
        SND_PCM_FORMAT_S16 = SND_PCM_FORMAT_S16_LE,
        /** Unsigned 16 bit CPU endian */
        SND_PCM_FORMAT_U16 = SND_PCM_FORMAT_U16_LE,
        /** Signed 24 bit CPU endian */
        SND_PCM_FORMAT_S24 = SND_PCM_FORMAT_S24_LE,
        /** Unsigned 24 bit CPU endian */
        SND_PCM_FORMAT_U24 = SND_PCM_FORMAT_U24_LE,
        /** Signed 32 bit CPU endian */
        SND_PCM_FORMAT_S32 = SND_PCM_FORMAT_S32_LE,
        /** Unsigned 32 bit CPU endian */
        SND_PCM_FORMAT_U32 = SND_PCM_FORMAT_U32_LE,
        /** Float 32 bit CPU endian */
        SND_PCM_FORMAT_FLOAT = SND_PCM_FORMAT_FLOAT_LE,
        /** Float 64 bit CPU endian */
        SND_PCM_FORMAT_FLOAT64 = SND_PCM_FORMAT_FLOAT64_LE,
        /** IEC-958 CPU Endian */
        SND_PCM_FORMAT_IEC958_SUBFRAME = SND_PCM_FORMAT_IEC958_SUBFRAME_LE
    }


    public static class AlsaNativeMethods
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="**pcm">snd_pcm_t**</param>
        /// <param name="name"></param>
        /// <param name="stream"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        [DllImport("asound.so.2")]
        public static extern int snd_pcm_open(out IntPtr pcm, string name, snd_pcm_stream_t stream, int mode);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="**ptr">snd_pcm_hw_params_t **</param>
        /// <returns></returns>
        [DllImport("asound.so.2")]
        public static extern int snd_pcm_hw_params_malloc(out IntPtr ptr);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="*pcm">snd_pcm_t *</param>
        /// <param name="*params">snd_pcm_hw_params_t *</param>
        /// <returns></returns>
        [DllImport("asound.so.2")]
        public static extern int snd_pcm_hw_params_any(IntPtr pcm, IntPtr hw_params);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcm">snd_pcm_t *</param>
        /// <param name="*params">snd_pcm_hw_params_t *</param>
        /// <param name="_access"></param>
        /// <returns></returns>
        [DllImport("asound.so.2")]
        public static extern int snd_pcm_hw_params_set_access(IntPtr pcm, IntPtr hw_params, snd_pcm_access_t access);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcm">snd_pcm_t *</param>
        /// <param name="hw_params">snd_pcm_hw_params_t *</param>
        /// <param name="format"></param>
        /// <returns></returns>
        [DllImport("asound.so.2")]
        public static extern int snd_pcm_hw_params_set_format(IntPtr pcm, IntPtr hw_params, snd_pcm_format_t format);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcm">snd_pcm_t *</param>
        /// <param name="hwParams">snd_pcm_hw_params_t *</param>
        /// <param name="*val"></param>
        /// <param name="*dir"></param>
        /// <returns></returns>
        [DllImport("asound.so.2")]
        public static extern int snd_pcm_hw_params_set_rate_near(IntPtr pcm, IntPtr hwParams, ref uint val, ref int dir);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcm">snd_pcm_t *</param>
        /// <param name="hwParams">snd_pcm_hw_params_t *</param>
        /// <param name="val"></param>
        /// <returns></returns>
        [DllImport("asound.so.2")]
        public static extern int snd_pcm_hw_params_set_channels(IntPtr pcm, IntPtr hwParams, uint val);

        [DllImport("asound.so.2")]
        public static extern int snd_pcm_hw_params_set_buffer_size(IntPtr pcm, IntPtr hwParams, uint val);

        [DllImport("asound.so.2")]
        public static extern int snd_pcm_hw_params_set_period_size(IntPtr pcm, IntPtr hwParams, uint val, int dir);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcm">snd_pcm_t *</param>
        /// <param name="hwParams">snd_pcm_hw_params_t *</param>
        /// <returns></returns>
        [DllImport("asound.so.2")]
        public static extern int snd_pcm_hw_params(IntPtr pcm, IntPtr hwParams);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwParams">snd_pcm_hw_params_t *</param>
        /// <returns></returns>
        [DllImport("asound.so.2")]
        public static extern void snd_pcm_hw_params_free(IntPtr obj);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcm">snd_pcm_t *</param>
        /// <returns></returns>
        [DllImport("asound.so.2")]
        public static extern int snd_pcm_prepare(IntPtr pcm);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcm"></param>
        /// <param name="buffer"></param>
        /// <param name="size">frames to read</param>
        /// <returns>frames read</returns>
        [DllImport("asound.so.2")]
        public static extern int snd_pcm_readi(IntPtr pcm, short[] buffer, uint size);
    }

    public class AlsaNativeError : Exception
    {
        public AlsaNativeError(int error, string method)
            : base($"Alsa error: {error} occured while calling {method}")
        {
            Error = error;
        }

        public int Error
        {
            get;
        }
    }
}