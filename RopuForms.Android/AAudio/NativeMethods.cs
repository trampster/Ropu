using System;
using System.Runtime.InteropServices;

namespace RopuForms.Droid.AAudio
{
    public static class NativeMethods
    {
        public const string AAudioLib = "aaudio";

        [DllImport(AAudioLib)]
        public static extern AAudioResult AAudio_createStreamBuilder(ref IntPtr streamBuilder);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setDeviceId(IntPtr builder, int deviceId);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setDirection(IntPtr builder, Direction direction);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setSharingMode(IntPtr builder, SharingMode sharingMode);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setSampleRate(IntPtr builder, int sampleRate);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setChannelCount(IntPtr builder, int channelCount);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setFormat(IntPtr builder, Format format);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setBufferCapacityInFrames(IntPtr builder, int numFrames);
    }
}