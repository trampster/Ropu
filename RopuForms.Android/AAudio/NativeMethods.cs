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

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setSamplesPerFrame(IntPtr builder, int samplesPerFrame);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setPerformanceMode(IntPtr buidler, PerformanceMode mode);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setUsage(IntPtr buidler, Usage mode);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setContentType(IntPtr buidler, ContentType mode);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setInputPreset(IntPtr buidler, InputPreset inputPreset);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setAllowedCapturePolicy(IntPtr buidler, AllowedCapturePolicy capturePolicy);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setSessionId(IntPtr builder, SessionId sessionId);

        public delegate void DataCallback(IntPtr stream, IntPtr userData, IntPtr audioData, int numFrames);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setDataCallback(IntPtr builder, DataCallback dataCallback, IntPtr userData);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setFramesPerDataCallback(IntPtr builder, int numFrames);

        public delegate void ErrorCallback(IntPtr stream, IntPtr userData, AAudioResult result);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setErrorCallback(IntPtr builder, ErrorCallback callback, IntPtr userData);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_openStream(IntPtr builder, out IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_delete(IntPtr builder);
    }
}