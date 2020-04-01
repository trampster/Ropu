namespace RopuForms.Droid.AAudio
{
    public enum AAudioResult
    {
        /**
         * The call was successful.
         */
        AAUDIO_OK,
        AAUDIO_ERROR_BASE = -900, // TODO review

        /**
         * The audio device was disconnected. This could occur, for example, when headphones
         * are plugged in or unplugged. The stream cannot be used after the device is disconnected.
         * Applications should stop and close the stream.
         * If this error is received in an error callback then another thread should be
         * used to stop and close the stream.
         */
        AAUDIO_ERROR_DISCONNECTED,

        /**
         * An invalid parameter was passed to AAudio.
         */
        AAUDIO_ERROR_ILLEGAL_ARGUMENT,
        // reserved
        AAUDIO_ERROR_INTERNAL = AAUDIO_ERROR_ILLEGAL_ARGUMENT + 2,

        /**
         * The requested operation is not appropriate for the current state of AAudio.
         */
        AAUDIO_ERROR_INVALID_STATE,
        // reserved
        // reserved
        /* The server rejected the handle used to identify the stream.
         */
        AAUDIO_ERROR_INVALID_HANDLE = AAUDIO_ERROR_INVALID_STATE + 3,
        // reserved

        /**
         * The function is not implemented for this stream.
         */
        AAUDIO_ERROR_UNIMPLEMENTED = AAUDIO_ERROR_INVALID_HANDLE + 2,

        /**
         * A resource or information is unavailable.
         * This could occur when an application tries to open too many streams,
         * or a timestamp is not available.
         */
        AAUDIO_ERROR_UNAVAILABLE,
        AAUDIO_ERROR_NO_FREE_HANDLES,

        /**
         * Memory could not be allocated.
         */
        AAUDIO_ERROR_NO_MEMORY,

        /**
         * A NULL pointer was passed to AAudio.
         * Or a NULL pointer was detected internally.
         */
        AAUDIO_ERROR_NULL,

        /**
         * An operation took longer than expected.
         */
        AAUDIO_ERROR_TIMEOUT,
        AAUDIO_ERROR_WOULD_BLOCK,

        /**
         * The requested data format is not supported.
         */
        AAUDIO_ERROR_INVALID_FORMAT,

        /**
         * A requested was out of range.
         */
        AAUDIO_ERROR_OUT_OF_RANGE,

        /**
         * The audio service was not available.
         */
        AAUDIO_ERROR_NO_SERVICE,

        /**
         * The requested sample rate was not supported.
         */
        AAUDIO_ERROR_INVALID_RATE
    }
}