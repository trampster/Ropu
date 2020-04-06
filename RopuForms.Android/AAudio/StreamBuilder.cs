using System;

namespace RopuForms.Droid.AAudio
{
    public class StreamBuilder : IDisposable
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
        /// The default, if you do not call this function, is SharingMode.Shared.
        /// The requested sharing mode may not be available.The application can query for the actual mode after the stream is opened.
        /// Available since API level 26.
        /// </summary>
        public SharingMode SharingMode
        {
            set => NativeMethods.AAudioStreamBuilder_setSharingMode(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Request a sample rate in Hertz.
        /// The default, if you do not call this function, is Contants.Unspecified. An optimal value will then be chosen when the stream is opened.After opening a stream with an unspecified value, the application must query for the actual value, which may vary by device.
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

        /// <summary>
        /// Identical to ChannelCount
        /// </summary>
        public int SamplesPerFrame
        { 
            set => NativeMethods.AAudioStreamBuilder_setSamplesPerFrame(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Set the requested performance mode.
        ///
        /// Supported modes are None, PowerSaving and LowLatency.
        ///
        /// The default, if you do not call this function, is None.
        ///
        /// You may not get the mode you requested.
        /// You can call AudioStream.PerformanceMode
        /// to find out the final mode for the stream.
        /// </summary>
        public PerformanceMode PerformanceMode
        {
            set => NativeMethods.AAudioStreamBuilder_setPerformanceMode(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Set the intended use case for the stream.
        /// The AAudio system will use this information to optimize the
        /// behavior of the stream.
        /// This could, for example, affect how volume and focus is handled for the stream.
        /// 
        /// The default, if you do not call this function, is Media.
        ///
        /// Added in API level 28.
        /// </summary>
        public Usage Usage
        {
            set => NativeMethods.AAudioStreamBuilder_setUsage(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Set the type of audio data that the stream will carry.
        ///
        /// The AAudio system will use this information to optimize the
        /// behavior of the stream.
        /// This could, for example, affect whether a stream is paused when a notification occurs.
        /// The default, if you do not call this function, is Music.
        /// 
        /// Added in API level 28.
        /// </summary>
        public ContentType ContentType
        {
            set => NativeMethods.AAudioStreamBuilder_setContentType(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Set the input(capture) preset for the stream.
        /// The AAudio system will use this information to optimize the
        /// behavior of the stream.
        /// This could, for example, affect which microphones are used and how the
        /// recorded data is processed.
        ///
        /// The default, if you do not call this function, is VoiceRecognition
        /// That is because VoiceRecognition is the preset with the lowest latency
        /// on many platforms.
        ///
        /// Added in API level 28.
        /// </summary>
        public InputPreset InputPreset
        {
            set => NativeMethods.AAudioStreamBuilder_setInputPreset(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Specify whether this stream audio may or may not be captured by other apps or the system.
        ///
        /// The default is AllowCaptureByAll.
        ///
        /// Note that an application can also set its global policy, in which case the most restrictive
        /// policy is always applied.
        /// 
        /// Added in API level 29.
        /// </summary>
        public AllowedCapturePolicy AllowedCapturePolicy
        {
            set => NativeMethods.AAudioStreamBuilder_setAllowedCapturePolicy(_streamBuilderPtr, value);
        }

        /// <summary>
        /// Set the requested session ID.
        ///
        /// The session ID can be used to associate a stream with effects processors.
        /// The effects are controlled using the Android AudioEffect Java API.
        ///
        /// The default, if you do not call this function, is None
        ///
        /// If set to Allocate then a session ID will be allocated
        /// when the stream is opened.
        ///
        /// The allocated session ID can be obtained by calling AudioStream.SessionId
        /// and then used with this function when opening another stream.
        ///
        /// This allows effects to be shared between streams.
        ///
        /// Session IDs from AAudio can be used with the Android Java APIs and vice versa.
        /// So a session ID from an AAudio stream can be passed to Java
        /// and effects applied using the Java AudioEffect API.
        ///
        /// Note that allocating or setting a session ID may result in a stream with higher latency.
        ///
        /// Allocated session IDs will always be positive and nonzero.
        ///
        /// Added in API level 28.
        /// </summary>
        public SessionId SessionId
        {
            set => NativeMethods.AAudioStreamBuilder_setSessionId(_streamBuilderPtr, value);
        }

        public struct AudioData
        {
            readonly IntPtr _audioDataPointer;
            readonly int _numFrames;

            public AudioData(IntPtr audioDataPointer, int numFrames)
            {
                _audioDataPointer = audioDataPointer;
                _numFrames = numFrames;
            }

            public int NumFrames
            {
                get => _numFrames;
            }

            public Span<float> AsFloat(int samplesPerFrame)
            {
                unsafe
                {
                    return new Span<float>(_audioDataPointer.ToPointer(), _numFrames * samplesPerFrame);
                }
            }

            public Span<short> AsShort(int samplesPerFrame)
            {
                unsafe
                {
                    return new Span<short>(_audioDataPointer.ToPointer(), _numFrames * samplesPerFrame);
                }
            }
        }

        /// <summary>
        /// Request that AAudio call this functions when the stream is running.
        ///
        /// Note that when using this callback, the audio data will be passed in or out
        /// of the function as an argument.
        /// So you cannot call Stream.Write() or Stream.Read()
        /// on the same stream that has an active data callback.
        ///
        /// The callback function will start being called after Stream.RequestStart()
        /// is called.
        /// It will stop being called after Stream.RequestPause() or
        /// AudioStream.RequestStop() is called.
        /// 
        /// This callback function will be called on a real-time thread owned by AAudio
        ///
        /// Note that the AAudio callbacks will never be called simultaneously from multiple threads.
        /// </summary>
        /// <param name="callback">Callback</param>
        public void SetAudioDataCallback(Action<AudioData> callback)
        {
            NativeMethods.DataCallback dataCallback = (streamPtr, userDataPtr, audioDataPtr, numFrames) =>
            {
                callback(new AudioData(audioDataPtr, numFrames));
            };
            NativeMethods.AAudioStreamBuilder_setDataCallback(_streamBuilderPtr, dataCallback, IntPtr.Zero);
        }


        /// <summary>
        /// Set the requested data callback buffer size in frames.
        ///
        /// The default, if you do not call this function, is Contants.Unspecified.
        ///
        /// For the lowest possible latency, do not call this function. AAudio will then
        /// call the dataProc callback function with whatever size is optimal.
        ///
        /// That size may vary from one callback to another.
        ///
        /// Only use this function if the application requires a specific number of frames for processing.
        ///
        /// The application might, for example, be using an FFT that requires
        /// a specific power-of-two sized buffer.
        ///
        /// AAudio may need to add additional buffering in order to adapt between the internal
        /// buffer size and the requested buffer size.
        ///
        /// If you do call this function then the requested size should be less than
        /// half the buffer capacity, to allow double buffering.
        /// </summary>
        public int FramesPerDataCallback
        {
            set => NativeMethods.AAudioStreamBuilder_setFramesPerDataCallback(_streamBuilderPtr, value);
        }
        
        /// <summary>
        /// Request that AAudio call this function if any error occurs or the stream is disconnected.
        ///
        /// It will be called, for example, if a headset or a USB device is unplugged causing the stream's
        /// device to be unavailable or "disconnected".
        /// Another possible cause of error would be a timeout or an unanticipated internal error.
        ///
        /// In response, this function should signal or create another thread to stop
        /// and close this stream. The other thread could then reopen a stream on another device.
        /// Do not stop or close the stream, or reopen the new stream, directly from this callback.
        ///
        /// This callback will not be called because of actions by the application, such as stopping
        /// or closing a stream.
        ///
        /// Note that the AAudio callbacks will never be called simultaneously from multiple threads.
        /// </summary>
        public Action<Result> ErrorCallback
        {
            set
            {
                NativeMethods.ErrorCallback callback = (stream, userData, result) => value(result);
                NativeMethods.AAudioStreamBuilder_setErrorCallback(_streamBuilderPtr, callback, IntPtr.Zero);
            }
        }

        /// <summary>
        /// Open a stream based on the options in the StreamBuilder.
        /// </summary>
        public Result OpenStream(out Stream stream)
        {
            IntPtr streamPtr;
            var result = NativeMethods.AAudioStreamBuilder_openStream(_streamBuilderPtr, out streamPtr);
            if (result == Result.OK)
            {
                stream = new Stream(streamPtr);
            }
            else
            {
                stream = null;
            }
            return result;
        }

        #region IDisposable Support
        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                NativeMethods.AAudioStreamBuilder_delete(_streamBuilderPtr);

                disposedValue = true;
            }
        }

        ~StreamBuilder()
        {
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}