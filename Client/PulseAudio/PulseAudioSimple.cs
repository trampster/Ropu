using System;
using System.Runtime.InteropServices;

namespace Ropu.Client.PulseAudio
{
    public class PulseAudioSimple : IAudioSource, IAudioPlayer, IDisposable
    {
        IntPtr _paSimple;
        public PulseAudioSimple(StreamDirection streamDirection, string streamName)
        {
            SampleSpec sampleSpec;
            sampleSpec.channels = 1;
            sampleSpec.format = SampleFormat.PA_SAMPLE_S16LE;
            sampleSpec.rate = 8000;

            IntPtr sampleSpecPtr = Marshal.AllocHGlobal(Marshal.SizeOf(sampleSpec));
            Marshal.StructureToPtr(sampleSpec, sampleSpecPtr, true);
            
            BufferAttributes bufferAttributes;
            bufferAttributes.maxlength = 160*12;
            bufferAttributes.tlength = 160*2;
            bufferAttributes.prebuf = 160*12;
            bufferAttributes.minreq = 160*2;
            bufferAttributes.fragsize = 160*2;
            IntPtr bufferAttributesPtr = Marshal.AllocHGlobal(Marshal.SizeOf(bufferAttributes));
            Marshal.StructureToPtr(bufferAttributes, bufferAttributesPtr, true);

            int error = 0;
            _paSimple = PulseAudioNativeMethods.pa_simple_new(null, "Ropu", streamDirection, null, streamName, sampleSpecPtr, IntPtr.Zero, bufferAttributesPtr, ref error);
            if(error != 0)
            {
                throw new Exception($"Failed to initalize pulse audio with error {error}");
            }
        }

        public ulong Latency
        {
            get
            {
                int error = 0;
                var latency = PulseAudioNativeMethods.pa_simple_get_latency(_paSimple, ref error);
                if(error != 0)
                {
                    throw new Exception($"Failed to get pulse audio latency error {error}");
                }
                return latency;
            }
        }

        public void PlayAudio(short[] buffer)
        {
            int error = 0;
            int result = PulseAudioNativeMethods.pa_simple_write(_paSimple, buffer, buffer.Length*2, ref error);
            if(error != 0 || result != 0)
            {
                throw new Exception($"Failed to write audio to PulseAudio Error: {error} Result: {result} ");
            }
        }

        public void ReadAudio(short[] buffer)
        {
            int error = 0;
            int result = PulseAudioNativeMethods.pa_simple_read(_paSimple, buffer, buffer.Length*2, ref error);
            if(error != 0 || result != 0)
            {
                IntPtr msgPtr = PulseAudioNativeMethods.pa_strerror(error);
                string? errorMessage = Marshal.PtrToStringAuto(msgPtr);
                throw new Exception($"Failed to read audio from PulseAudio Error: {errorMessage} Result: {result} ");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                if(_paSimple != IntPtr.Zero)
                {
                    PulseAudioNativeMethods.pa_simple_free(_paSimple);
                    _paSimple = IntPtr.Zero;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}