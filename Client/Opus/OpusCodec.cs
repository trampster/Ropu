using System;
using System.Runtime.InteropServices;
using Ropu.Client.JitterBuffer;

namespace Ropu.Client.Opus
{
    public class OpusCodec : IAudioCodec
    {
        IntPtr _opusEncoderPtr;
        IntPtr _opusDecoderPtr;
        public OpusCodec()
        {
            ErrorCodes errorCode = ErrorCodes.OPUS_OK;
            _opusEncoderPtr = OpusNativeMethods.opus_encoder_create(8000, 1, OpusApplication.OPUS_APPLICATION_VOIP, ref errorCode);
            if(errorCode != ErrorCodes.OPUS_OK)
            {
                throw new Exception($"Failed to create Opus Encoder with error {errorCode}");
            }
            if(OpusNativeMethods.opus_encoder_ctl(_opusEncoderPtr, EncoderCtlOptions.OPUS_SET_INBAND_FEC_REQUEST, 1) != ErrorCodes.OPUS_OK)
            {
                throw new Exception($"Failed to enable inband Forward Error Correction");
            }
            if(OpusNativeMethods.opus_encoder_ctl(_opusEncoderPtr, EncoderCtlOptions.OPUS_SET_PACKET_LOSS_PERC_REQUEST, 30) != ErrorCodes.OPUS_OK)
            {
                throw new Exception($"Failed to set pakcet loss on opus encoder");
            }

            _opusDecoderPtr = OpusNativeMethods.opus_decoder_create(8000, 1, ref errorCode);
            if(errorCode != ErrorCodes.OPUS_OK)
            {
                throw new Exception($"Failed to create Opus Decoder with error {errorCode}");
            }
        }

        public int Decode(AudioData audioData, bool isNext, short[] output)
        {
            int size = OpusNativeMethods.opus_decode(
                _opusDecoderPtr, 
                audioData?.Buffer, 
                audioData != null ? audioData.Length : 0, 
                output, 
                160, 
                isNext ? 1 : 0);
            if(size != 160)
            {
                throw new Exception($"Failed to decode with opus with error {(ErrorCodes)size}");
            }
            return size;
        }


        public int Encode(short[] raw, Span<byte> output)
        {
            int size = OpusNativeMethods.opus_encode(_opusEncoderPtr, raw, 160, ref MemoryMarshal.GetReference(output), output.Length);
            if(size < 0)
            {
                throw new Exception($"Failed to encode with opus with error {(ErrorCodes)size}");
            }
            return size;
        }
    }
}