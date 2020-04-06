using System;
using System.Runtime.InteropServices;

namespace RopuForms.Droid.AAudio
{
    public static class NativeMethods
    {
        public const string AAudioLib = "aaudio";

        [DllImport(AAudioLib)]
        public static extern Result AAudio_createStreamBuilder(ref IntPtr streamBuilder);

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

        public delegate void ErrorCallback(IntPtr stream, IntPtr userData, Result result);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_setErrorCallback(IntPtr builder, ErrorCallback callback, IntPtr userData);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStreamBuilder_openStream(IntPtr builder, out IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern void AAudioStreamBuilder_delete(IntPtr builder);

        [DllImport(AAudioLib)]
        public static extern void AAudioStream_close(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStream_requestStart(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStream_requestPause(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStream_requestFlush(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStream_requestStop(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern StreamState AAudioStream_getState(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStream_waitForStateChange(IntPtr stream, StreamState inputState, out StreamState nextState, long timeoutNanoseconds);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStream_read(IntPtr stream, IntPtr buffer, int numFrames, long timeoutNanoseconds);
        
        //[DllImport(AAudioLib)]
        //public static extern Result AAudioStream_read(IntPtr stream, float[] buffer, int numFrames, long timeoutNanoseconds);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStream_write(IntPtr stream, short[] buffer, int numFrames, long timeoutNanoseconds);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStream_write(IntPtr stream, float[] buffer, int numFrames, long timeoutNanoseconds);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStream_setBufferSizeInFrames(IntPtr stream, int numFrame);

        [DllImport(AAudioLib)]
        public static extern int AAudioStream_getBufferSizeInFrames(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern int AAudioStream_getFramesPerBurst(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern int AAudioStream_getBufferCapacityInFrames(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern int AAudioStream_getFramesPerDataCallback(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern int AAudioStream_getXRunCount(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern int AAudioStream_getSampleRate(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern int AAudioStream_getChannelCount(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern int AAudioStream_getSamplesPerFrame(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern Format AAudioStream_getFormat(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern SharingMode AAudioStream_getSharingMode(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern PerformanceMode AAudioStream_getPerformanceMode(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern Direction AAudioStream_getDirection(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern long AAudioStream_getFramesWritten(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern long AAudioStream_getFramesRead(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern SessionId AAudioStream_getSessionId(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern Result AAudioStream_getTimestamp(IntPtr stream, ClockId clockId, out long framePosition, out long timeNanoseconds);

        [DllImport(AAudioLib)]
        public static extern Usage AAudioStream_getUsage(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern ContentType AAudioStream_getContentType(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern InputPreset AAudioStream_getInputPreset(IntPtr stream);

        [DllImport(AAudioLib)]
        public static extern AllowedCapturePolicy AAudioStream_getAllowedCapturePolicy(IntPtr stream);
    }
}