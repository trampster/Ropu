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
            return NativeMethods.AAudioStream_read(_streamPtr, buffer, numFrames, timeoutNanoseconds);
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
            return NativeMethods.AAudioStream_read(_streamPtr, buffer, numFrames, timeoutNanoseconds);
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