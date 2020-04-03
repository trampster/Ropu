namespace RopuForms.Droid.AAudio
{
    public enum Result
    {
        /// <summary>
        /// The call was successful.
        /// </summary>
        OK,
        ErrorBase = -900, // TODO review

        /// <summary>
        /// The audio device was disconnected. This could occur, for example, when headphones
        /// are plugged in or unplugged. The stream cannot be used after the device is disconnected.
        /// Applications should stop and close the stream.
        /// If this error is received in an error callback then another thread should be
        /// used to stop and close the stream.
        /// </summary>
        ErrorDisconnected,

        /// <summary>
        /// An invalid parameter was passed to AAudio.
        /// </summary>
        ErrorIllegalArgument,
        // reserved
        ErrorInternal = ErrorIllegalArgument + 2,

        /**
         * The requested operation is not appropriate for the current state of AAudio.
         */
        ErrorInvalidState,
        // reserved
        // reserved

        /// <summary>
        /// The server rejected the handle used to identify the stream.
        /// </summary>
        ErrorInvalidHandle = ErrorInvalidState + 3,
        // reserved

        /// <summary>
        /// The function is not implemented for this stream.
        /// </summary>
        ErrorUnimplemented = ErrorInvalidHandle + 2,

        /// <summary>
        /// A resource or information is unavailable.
        /// This could occur when an application tries to open too many streams,
        /// or a timestamp is not available.
        /// </summary>
        ErrorUnavailble,
        ErrorNotFreeHandles,

        /// <summary>
        /// Memory could not be allocated.
        /// </summary>
        ErrorNoMemory,

        /// <summary>
        /// A NULL pointer was passed to AAudio.
        /// Or a NULL pointer was detected internally.
        /// </summary>
        ErrorNull,

        /// <summary>
        /// An operation took longer than expected. 
        /// </summary>
        ErrorTimeout,
        ErrorWouldBlock,

        /// <summary>
        /// The requested data format is not supported.
        /// </summary>
        ErrorInvalidFormat,

        /// <summary>
        /// A requested was out of range.
        /// </summary>
        ErrorOutOfRange,

        /// <summary>
        /// The audio service was not available.
        /// </summary>
        ErrorNotService,

        /// <summary>
        /// The requested sample rate was not supported.
        /// </summary>
        ErrorInvalidRate
    }
}