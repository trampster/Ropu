using System;
using System.Runtime.InteropServices;

namespace Ropu.Client.Opus
{
    public enum OpusApplication
    {
        OPUS_APPLICATION_VOIP = 2048,
        OPUS_APPLICATION_AUDIO = 2049,
        OPUS_APPLICATION_RESTRICTED_LOWDELAY = 2049
    }

    public enum ErrorCodes : int
    {
        OPUS_OK = 0,
        OPUS_BAD_ARG = -1,
        OPUS_BUFFER_TOO_SMALL = -2,
        OPUS_INTERNAL_ERROR = -3,
        OPUS_INVALID_PACKET = -4,
        OPUS_UNIMPLEMENTED = -5,
        OPUS_INVALID_STATE = -6,
        OPUS_ALLOC_FAIL = -7
    }

    public enum EncoderCtlOptions
    {
        OPUS_SET_INBAND_FEC_REQUEST = 4012,
        OPUS_SET_PACKET_LOSS_PERC_REQUEST = 4014
    }


    public static class OpusNativeMethods
    {
        const string OPUS_LIB = "opus";
        /// <summary>
        /// Allocates and initializes an encoder state.
        /// </summary>
        /// <param name="fs">Sampling rate of input signal (Hz) This must be one of 8000, 12000, 16000, 24000, or 48000.</param>
        /// <param name="channels">Number of channels (1 or 2) in input signal</param>
        /// <param name="application">Coding mode</param>
        /// <param name="error">errort codes</param>
        /// <returns>Encoder state</returns>
        [DllImport(OPUS_LIB)]
        public static extern IntPtr opus_encoder_create (int fs, int channels, OpusApplication application, ref ErrorCodes error);

        /// <summary>
        /// Encodes an Opus frame.
        /// </summary>
        /// <param name="st">Encoder state</param>
        /// <param name="pcm">Input signal (interleaved if 2 channels). length is frame_size*channels</param>
        /// <param name="frame_size">Number of samples per channel in the input signal. This must be an Opus frame size for the encoder's sampling rate. For example, at 48 kHz the permitted values are 120, 240, 480, 960, 1920, and 2880. Passing in a duration of less than 10 ms (480 samples at 48 kHz) will prevent the encoder from using the LPC or hybrid modes.</param>
        /// <param name="data">Output payload. This must contain storage for at least max_data_bytes</param>
        /// <param name="max_data_bytes">Size of the allocated memory for the output payload. This may be used to impose an upper limit on the instant bitrate, but should not be used as the only bitrate control. Use OPUS_SET_BITRATE to control the bitrate.</param>
        /// <returns>The length of the encoded packet (in bytes) on success or a negative error code (ErrorCodes) on failure</returns>
        [DllImport(OPUS_LIB)]
        public static extern int opus_encode (IntPtr st, short[] pcm, int frame_size, ref byte data, int max_data_bytes);

        /// <summary>
        /// Frees an OpusEncoder allocated by opus_encoder_create().
        /// </summary>
        /// <param name="st">State to be freed.</param>
        [DllImport(OPUS_LIB)]
        public static extern void opus_encoder_destroy(IntPtr st);

        /// <summary>
        /// Frees an OpusDecoder allocated by opus_decoder_create().
        /// </summary>
        /// <param name="st">State to be freed.</param>
        [DllImport(OPUS_LIB)]
        public static extern void opus_decoder_destroy(IntPtr st);

        [DllImport(OPUS_LIB)]
        public static extern ErrorCodes opus_encoder_ctl(IntPtr st, EncoderCtlOptions request, int value);

        /// <summary>
        /// Allocates and initializes a decoder state.
        /// </summary>
        /// <param name="fs">Sample rate to decode at (Hz). This must be one of 8000, 12000, 16000, 24000, or 48000.</param>
        /// <param name="channels">Number of channels (1 or 2) to decode</param>
        /// <param name="error">OPUS_OK Success or ErrorCodes</param>
        /// <returns>pointer to OpusDecoder</returns>
        [DllImport(OPUS_LIB)]
        public static extern IntPtr opus_decoder_create(int fs, int channels, ref ErrorCodes error);

        /// <summary>
        /// Decoder state
        /// </summary>
        /// <param name="st">OpusDecoder</param>
        /// <param name="data">Input payload. Use a NULL pointer to indicate packet loss</param>
        /// <param name="len">Number of bytes in payload</param>
        /// <param name="output">Output signal (interleaved if 2 channels). length is frame_size*channels</param>
        /// <param name="frame_size">Number of samples per channel of available space in pcm. If this is less than the maximum packet duration (120ms; 5760 for 48kHz), this function will not be capable of decoding some packets. In the case of PLC (data==NULL) or FEC (decode_fec=1), then frame_size needs to be exactly the duration of audio that is missing, otherwise the decoder will not be in the optimal state to decode the next incoming packet. For the PLC and FEC cases, frame_size must be a multiple of 2.5 ms.</param>
        /// <param name="decode_fec">Flag (0 or 1) to request that any in-band forward error correction data be decoded. If no such data is available, the frame is decoded as if it were lost.</param>
        /// <returns>Number of decoded samples or ErrorCodes</returns>
        [DllImport(OPUS_LIB)]
        public static extern int opus_decode (IntPtr st, byte[]? data, int len, short[] output, int frame_size, int decode_fec);
    }
}