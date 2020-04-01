using System;

namespace RopuForms.Droid.AAudio
{
    public class StreamBuilder
    {
        readonly IntPtr _streamBuilderPtr;
        public StreamBuilder()
        {
            _streamBuilderPtr = IntPtr.Zero;
            NativeMethods.AAudio_createStreamBuilder(ref _streamBuilderPtr);
        }

        /// <summary>
        /// Request an audio device identified device using an ID.
        /// On Android, for example, the ID could be obtained from the Java AudioManager.
        /// The default, if you do not call this function, is AAUDIO_UNSPECIFIED, in which case the primary device will be used.
        /// Available since API level 26.
        /// </summary>
        public int DeviceId
        {
            set => NativeMethods.AAudioStreamBuilder_setDeviceId(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Request the direction for a stream.
        /// The default, if you do not call this function, is Output.
        /// Available since API level 26.
        /// </summary>
        public Direction Direction
        {
            set => NativeMethods.AAudioStreamBuilder_setDirection(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Request a mode for sharing the device.
        /// The default, if you do not call this function, is AAUDIO_SHARING_MODE_SHARED.
        /// The requested sharing mode may not be available.The application can query for the actual mode after the stream is opened.
        /// Available since API level 26.
        /// </summary>
        public SharingMode SharingMode
        {
            set => NativeMethods.AAudioStreamBuilder_setSharingMode(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Request a sample rate in Hertz.
        /// The default, if you do not call this function, is AAUDIO_UNSPECIFIED.An optimal value will then be chosen when the stream is opened.After opening a stream with an unspecified value, the application must query for the actual value, which may vary by device.
        /// If an exact value is specified then an opened stream will use that value.If a stream cannot be opened with the specified value then the open will fail.
        /// Available since API level 26.
        /// </summary>
        public int SampleRate
        {
            set => NativeMethods.AAudioStreamBuilder_setSampleRate(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Request a number of channels for the stream.
        /// The default, if you do not call this function, is AAUDIO_UNSPECIFIED.An optimal value will then be chosen when the stream is opened.After opening a stream with an unspecified value, the application must query for the actual value, which may vary by device.
        /// If an exact value is specified then an opened stream will use that value.If a stream cannot be opened with the specified value then the open will fail.
        /// Available since API level 26.
        /// </summary>
        public int ChannelCount
        {
            set => NativeMethods.AAudioStreamBuilder_setChannelCount(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Request a sample data format, for example AAUDIO_FORMAT_PCM_I16.
        /// The default, if you do not call this function, is Unspecified. An optimal value will then be chosen when the stream is opened. After opening a stream with an unspecified value, the application must query for the actual value, which may vary by device.
        /// If an exact value is specified then an opened stream will use that value. If a stream cannot be opened with the specified value then the open will fail.
        /// Available since API level 26.
        /// </summary>
        public Format Format
        {
            set => NativeMethods.AAudioStreamBuilder_setFormat(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Set the requested buffer capacity in frames.
        /// The final AAudioStream capacity may differ, but will probably be at least this big.
        /// The default, if you do not call this function, is AAUDIO_UNSPECIFIED.
        /// Available since API level 26.
        /// </summary>
        public int NumFrames
        {
            set => NativeMethods.AAudioStreamBuilder_setBufferCapacityInFrames(_streamBuilderPtr, value);
        }
    }
}