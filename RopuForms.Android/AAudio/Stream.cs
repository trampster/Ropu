using System;

namespace RopuForms.Droid.AAudio
{
    public class Stream : IDisposable
    {
        readonly IntPtr _streamPtr;

        public Stream(IntPtr streamPtr)
        {
            _streamPtr = streamPtr;
        }

        /// <summary>
        /// Asynchronously request to start playing the stream. For output streams, one should
        /// write to the stream to fill the buffer before starting.
        /// Otherwise it will underflow.
        /// After this call the state will be in StreamState.Starting or StreamState.Started
        /// </summary>
        public Result RequestStart()
        {
            return NativeMethods.AAudioStream_requestStart(_streamPtr);
        }

        /// <summary>
        /// Asynchronous request for the stream to pause.
        /// Pausing a stream will freeze the data flow but not flush any buffers.
        /// Use AAudioStream_requestStart() to resume playback after a pause.
        /// After this call the state will be in StreamState.Pausing or StreamState.Paused
        ///  
        /// This will return Result.ErrorUnimplemented for input streams.
        /// For input streams use RequestStop().
        /// </summary>
        public Result RequestPause()
        {
            return NativeMethods.AAudioStream_requestPause(_streamPtr);
        }

        /// <summary>
        /// Asynchronous request for the stream to flush.
        /// Flushing will discard any pending data.
        /// This call only works if the stream is pausing or paused.TODO review
        /// Frame counters are not reset by a flush. They may be advanced.
        /// After this call the state will be in
        /// StreamState.Flusing or StreamState.Flushed
        ///
        /// This will return Result.ErrorUnimplemented for input streams.
        /// </summary>
        public Result RequestFlush()
        {
            return NativeMethods.AAudioStream_requestFlush(_streamPtr);
        }

        /// <summary>
        /// Asynchronous request for the stream to stop.
        /// The stream will stop after all of the data currently buffered has been played.
        /// After this call the state will be in StreamState.Stopping or StreamState.Stopped
        /// </summary>
        public Result RequestStop()
        {
            return NativeMethods.AAudioStream_requestStop(_streamPtr);
        }

        /// <summary>
        /// Query the current state of the client, eg. {@link #AAUDIO_STREAM_STATE_PAUSING}
        ///
        /// This function will immediately return the state without updating the state.
        /// If you want to update the client state based on the server state then
        /// call Stream.WaitForStateChange() with currentState set to StreamState. Unknown 
        /// and a zero timeout.
        /// </summary>
        public StreamState State
        {
            get => NativeMethods.AAudioStream_getState(_streamPtr);
        }

        /// <summary>
        /// Wait until the current state no longer matches the input state.
        /// 
        /// This will update the current client state.
        /// 
        /// aaudio_result_t result = AAUDIO_OK;
        /// aaudio_stream_state_t currentState = AAudioStream_getState(stream);
        /// aaudio_stream_state_t inputState = currentState;
        /// while (result == Result.OK && currentState != StreamState.Paused) {
        ///     result = WaitForStateChange(
        ///               inputState, out currentState, MY_TIMEOUT_NANOS);
        ///     inputState = currentState;
        /// }
        /// </summary>
        /// <param name="inputState">The state we want to avoid.</param>
        /// <param name="nextState">Variable that will be set to the new state.</param>
        /// <param name="timeoutNanoseconds">Maximum number of nanoseconds to wait for completion.</param>
        /// <returns>OK or a negative error</returns>
        public Result WaitForStateChange(StreamState inputState, out StreamState nextState, long timeoutNanoseconds)
        {
            return NativeMethods.AAudioStream_waitForStateChange(_streamPtr, inputState, out nextState, timeoutNanoseconds);
        }

        /// <summary>
        /// Read data from the stream.
        ///
        /// The call will wait until the read is complete or until it runs out of time.
        /// If timeoutNanos is zero then this call will not wait.
        ///
        /// Note that timeoutNanoseconds is a relative duration in wall clock time.
        /// Time will not stop if the thread is asleep.
        /// So it will be implemented using CLOCK_BOOTTIME.
        ///
        /// This call is "strong non-blocking" unless it has to wait for data.
        ///
        /// If the call times out then zero or a partial frame count will be returned.
        /// </summary>
        /// <param name="buffer">Buffer to fill with audio samples</param>
        /// <param name="numFrames">Number of frames to read. Only complete frames will be written</param>
        /// <param name="timeoutNanoseconds">Maximum number of nanoseconds to wait for completion.</param>
        /// <returns>The number of frames actually read or a negative error.</returns>
        public Result Read(short[] buffer, int numFrames, long timeoutNanoseconds)
        {
            Console.WriteLine($"Stream.Read IntPtr = {_streamPtr.ToInt64()}");
            unsafe
            {
                fixed (short* pByte = buffer)
                {
                    IntPtr intPtr = new IntPtr((void*)pByte);
                    return NativeMethods.AAudioStream_read(_streamPtr, intPtr, numFrames, timeoutNanoseconds);
                }
            }
        }

        /// <summary>
        /// Read data from the stream.
        ///
        /// The call will wait until the read is complete or until it runs out of time.
        /// If timeoutNanos is zero then this call will not wait.
        ///
        /// Note that timeoutNanoseconds is a relative duration in wall clock time.
        /// Time will not stop if the thread is asleep.
        /// So it will be implemented using CLOCK_BOOTTIME.
        ///
        /// This call is "strong non-blocking" unless it has to wait for data.
        ///
        /// If the call times out then zero or a partial frame count will be returned.
        /// </summary>
        /// <param name="buffer">Buffer to fill with audio samples</param>
        /// <param name="numFrames">Number of frames to read. Only complete frames will be written</param>
        /// <param name="timeoutNanoseconds">Maximum number of nanoseconds to wait for completion.</param>
        /// <returns>The number of frames actually read or a negative error.</returns>
        public Result Read(float[] buffer, int numFrames, long timeoutNanoseconds)
        {
            unsafe
            {
                fixed (float* pByte = buffer)
                {
                    IntPtr intPtr = new IntPtr((void*)pByte);
                    return NativeMethods.AAudioStream_read(_streamPtr, intPtr, numFrames, timeoutNanoseconds);
                }
            }
        }

        /// <summary>
        /// Write data to the stream.
        ///
        /// The call will wait until the write is complete or until it runs out of time.
        /// If timeoutNanos is zero then this call will not wait.
        ///
        /// Note that timeoutNanoseconds is a relative duration in wall clock time.
        /// Time will not stop if the thread is asleep.
        /// So it will be implemented using CLOCK_BOOTTIME.
        ///
        /// This call is "strong non-blocking" unless it has to wait for room in the buffer.
        ///
        /// If the call times out then zero or a partial frame count will be returned.
        /// </summary>
        /// <param name="buffer">Buffer with audio samples to write</param>
        /// <param name="numFrames">Number of frames to write. Only complete frames will be written.</param>
        /// <param name="timeoutNanoseconds">Maximum number of nanoseconds to wait for completion.</param>
        /// <returns>The number of frames actually written or a negative error.</returns>
        public Result Write(short[] buffer, int numFrames, long timeoutNanoseconds)
        {
            return NativeMethods.AAudioStream_write(_streamPtr, buffer, numFrames, timeoutNanoseconds);
        }

        /// <summary>
        /// Write data to the stream.
        ///
        /// The call will wait until the write is complete or until it runs out of time.
        /// If timeoutNanos is zero then this call will not wait.
        ///
        /// Note that timeoutNanoseconds is a relative duration in wall clock time.
        /// Time will not stop if the thread is asleep.
        /// So it will be implemented using CLOCK_BOOTTIME.
        ///
        /// This call is "strong non-blocking" unless it has to wait for room in the buffer.
        ///
        /// If the call times out then zero or a partial frame count will be returned.
        /// </summary>
        /// <param name="buffer">Buffer with audio samples to write</param>
        /// <param name="numFrames">Number of frames to write. Only complete frames will be written.</param>
        /// <param name="timeoutNanoseconds">Maximum number of nanoseconds to wait for completion.</param>
        /// <returns>The number of frames actually written or a negative error.</returns>
        public Result Write(float[] buffer, int numFrames, long timeoutNanoseconds)
        {
            return NativeMethods.AAudioStream_write(_streamPtr, buffer, numFrames, timeoutNanoseconds);
        }

        /// <summary>
        /// This can be used to adjust the latency of the buffer by changing
        /// the threshold where blocking will occur.
        /// By combining this with get Stream.XRunCount, the latency can be tuned
        /// at run-time for each device.
        ///
        /// This cannot be set higher than Stream.BufferCapacityInFrames.
        ///
        /// Note that you will probably not get the exact size you request.
        /// You can check the return value or call AAudioStream_getBufferSizeInFrames()
        /// to see what the actual final size is.
        /// </summary>
        /// <param name="numFrames">requested number of frames that can be filled without blocking</param>
        /// <returns>actual buffer size in frames or a negative error</returns>
        public Result SetBufferSizeInFrames(int numFrames)
        {
            return NativeMethods.AAudioStream_setBufferSizeInFrames(_streamPtr, numFrames);
        }

        /// <summary>
        /// Query the maximum number of frames that can be filled without blocking.
        /// </summary>
        public int BufferSizeInFrames
        {
            get => NativeMethods.AAudioStream_getBufferSizeInFrames(_streamPtr);
        }

        /// <summary>
        /// Query the number of frames that the application should read or write at
        /// one time for optimal performance.It is OK if an application writes
        /// a different number of frames. But the buffer size may need to be larger
        /// in order to avoid underruns or overruns.
        ///
        /// Note that this may or may not match the actual device burst size.
        /// For some endpoints, the burst size can vary dynamically.
        /// But these tend to be devices with high latency.
        /// </summary>
        public int FramesPerBurst
        {
            get => NativeMethods.AAudioStream_getFramesPerBurst(_streamPtr);
        }

        /// <summary>
        /// Query maximum buffer capacity in frames.
        /// </summary>
        public int BufferCapacityInFrames
        {
            get => NativeMethods.AAudioStream_getBufferCapacityInFrames(_streamPtr);
        }

        /// <summary>
        /// Query the size of the buffer that will be passed to the dataProc callback
        /// in the numFrames parameter.
        ///
        /// This call can be used if the application needs to know the value of numFrames before
        /// the stream is started.This is not normally necessary.
        ///
        /// If a specific size was requested by calling
        /// AAudioStreamBuilder_setFramesPerDataCallback() then this will be the same size.
        ///
        /// If AAudioStreamBuilder_setFramesPerDataCallback() was not called then this will
        /// return the size chosen by AAudio, or Constants.Unspecified
        ///
        /// Constants.Unspecified indicates that the callback buffer size for this stream
        /// may vary from one dataProc callback to the next.
        /// </summary>
        public int FramesPerDataCallback
        {
            get => NativeMethods.AAudioStream_getFramesPerDataCallback(_streamPtr);
        }

        /// <summary>
        /// An XRun is an Underrun or an Overrun.
        /// During playing, an underrun will occur if the stream is not written in time
        /// and the system runs out of valid data.
        /// During recording, an overrun will occur if the stream is not read in time
        /// and there is no place to put the incoming data so it is discarded.
        ///
        /// An underrun or overrun can cause an audible "pop" or "glitch".
        ///
        /// Note that some INPUT devices may not support this function.
        /// In that case a 0 will always be returned.
        /// </summary>
        public int XRunCount
        {
            get => NativeMethods.AAudioStream_getXRunCount(_streamPtr);
        }

        /// <summary>
        /// actual sample rate
        /// </summary>
        public int SampleRate
        {
            get => NativeMethods.AAudioStream_getSampleRate(_streamPtr);
        }

        /// <summary>
        /// A stream has one or more channels of data.
        /// A frame will contain one sample for each channel.
        /// </summary>
        public int ChannelCount
        {
            get => NativeMethods.AAudioStream_getChannelCount(_streamPtr);
        }

        /// <summary>
        /// Identical to ChannelCount.
        /// </summary>
        public int SamplesPerFrame
        {
            get => NativeMethods.AAudioStream_getSamplesPerFrame(_streamPtr);
        }

        /// <summary>
        /// actual data format
        /// </summary>
        public Format Format
        {
            get => NativeMethods.AAudioStream_getFormat(_streamPtr);
        }

        /// <summary>
        /// Provide actual sharing mode.
        /// </summary>
        public SharingMode SharingMode
        {
            get => NativeMethods.AAudioStream_getSharingMode(_streamPtr);
        }

        /// <summary>
        /// Get the performance mode used by the stream.
        /// </summary>
        public PerformanceMode PerformanceMode
        {
            get => NativeMethods.AAudioStream_getPerformanceMode(_streamPtr);
        }

        /// <summary>
        /// direction
        /// </summary>
        public Direction Direction
        {
            get => NativeMethods.AAudioStream_getDirection(_streamPtr);
        }

        /// <summary>
        /// Passes back the number of frames that have been written since the stream was created.
        /// For an output stream, this will be advanced by the application calling write()
        /// or by a data callback.
        /// For an input stream, this will be advanced by the endpoint.
        ///
        /// The frame position is monotonically increasing.
        /// </summary>
        public long FramesWritten
        {
            get => NativeMethods.AAudioStream_getFramesWritten(_streamPtr);
        }


        /// <summary>
        /// Passes back the number of frames that have been read since the stream was created.
        /// For an output stream, this will be advanced by the endpoint.
        /// For an input stream, this will be advanced by the application calling read()
        /// or by a data callback.
        ///
        /// The frame position is monotonically increasing.
        /// </summary>
        public long FramesRead
        {
            get => NativeMethods.AAudioStream_getFramesRead(_streamPtr);
        }

        /// <summary>
        /// Passes back the session ID associated with this stream.
        ///
        /// The session ID can be used to associate a stream with effects processors.
        /// The effects are controlled using the Android AudioEffect Java API.
        ///
        /// If StreamBuilder.SessionId was set to SessionId.Allocate
        /// then a new session ID should be allocated once when the stream is opened.
        ///
        /// If StreamBuilder.SessionId was set with a previously allocated
        /// session ID then that value should be returned.
        ///
        ///
        /// If StreamBuilder.SessionId was not set then this function should
        /// return SessionId.None.
        ///
        /// The sessionID for a stream should not change once the stream has been opened.
        /// 
        /// Added in API level 28.
        /// </summary>
        public SessionId SessionId
        {
            get => NativeMethods.AAudioStream_getSessionId(_streamPtr);
        }

        /// <summary>
        /// Passes back the time at which a particular frame was presented.
        /// This can be used to synchronize audio with video or MIDI.
        /// It can also be used to align a recorded stream with a playback stream.
        ///
        /// Timestamps are only valid when the stream is in StreamState.Started.
        /// Result.ErrorInvalidState will be returned if the stream is not started.
        /// Note that because RequestStart() is asynchronous, timestamps will not be valid until
        /// a short time after calling RequestStart().
        /// So Result.ErrorInvalidState should not be considered a fatal error.
        /// Just try calling again later.
        ///
        /// If an error occurs, then the position and time will not be modified.
        ///
        /// The position and time passed back are monotonically increasing.
        /// </summary>
        /// <param name="clockId">LOCK_MONOTONIC or CLOCK_BOOTTIME</param>
        /// <param name="framePosition">variable to receive the position</param>
        /// <param name="timeNanoseconds">variable to receive the time</param>
        /// <returns>OK or a negative error</returns>
        public Result GetTimestamp(ClockId clockId, out long framePosition, out long timeNanoseconds)
        {
            return NativeMethods.AAudioStream_getTimestamp(_streamPtr, clockId, out framePosition, out timeNanoseconds);
        }

        /// <summary>
        /// Return the use case for the stream.
        ///
        /// Added in API level 28.
        /// </summary>
        public Usage Usage
        {
            get => NativeMethods.AAudioStream_getUsage(_streamPtr);
        }

        /// <summary>
        /// Return the content type for the stream.
        ///
        /// Added in API level 28.
        /// </summary>
        public ContentType ContentType
        {
            get => NativeMethods.AAudioStream_getContentType(_streamPtr);
        }

        /// <summary>
        /// Return the input preset for the stream.
        ///
        /// Added in API level 28.
        /// </summary>
        public InputPreset InputPreset
        {
            get => NativeMethods.AAudioStream_getInputPreset(_streamPtr);
        }

        /// <summary>
        /// Return the policy that determines whether the audio may or may not be captured
        /// by other apps or the system.
        ///
        /// Added in API level 29.
        /// </summary>
        public AllowedCapturePolicy AllowedCapturePolicy
        {
            get => NativeMethods.AAudioStream_getAllowedCapturePolicy(_streamPtr);
        }

        #region IDisposable Support
        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                NativeMethods.AAudioStream_close(_streamPtr);

                disposedValue = true;
            }
        }

        ~Stream()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}