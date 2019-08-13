using System;
using System.Runtime.InteropServices;

namespace Ropu.Client.PulseAudio
{
    public static class PulseAudioNativeMethods
    {

        [DllImport("pulse-simple.so.0")]
        public static extern IntPtr pa_simple_new(
            string server, //const char * 	server
            string name, //const char * 	name
            StreamDirection direction, // pa_stream_direction_t 	dir,
            string dev, // const char * 	dev,
            string streamName, //const char * 	stream_name,
            IntPtr sampleSpec, // pointer to struct SampleSpec
            IntPtr channelMap, // Channel Map use pass IntPtr.Zero for now
            IntPtr bufferAttributes, // Playback and record buffer metrics, pass IntPtr.Zero for now
            ref int error);

        [DllImport("pulse-simple.so.0")]
        public static extern void pa_simple_free(IntPtr simple);

        [DllImport("pulse-simple.so.0")]
        public static extern ulong pa_simple_get_latency(IntPtr simple, ref int error);


        [DllImport("pulse-simple.so.0")]
        public static extern int pa_simple_read(IntPtr simple, short[] data, long bytes, ref int error);

        [DllImport("pulse-simple.so.0")]
        public static extern int pa_simple_write(IntPtr simple, short[] data, long bytes, ref int error);

        [DllImport("pulse-simple.so.0")]
        public static extern IntPtr pa_strerror(int error);
    }
}