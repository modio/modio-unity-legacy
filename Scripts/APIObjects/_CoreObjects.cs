using System;

namespace ModIO.API
{
    // --------- BASE API OBJECTS ---------
    [Serializable]
    public struct MessageObject
    {
        // - Fields -
        public int code;    // HTTP status code of response.
        public string message; // The server response to your request. Responses will vary depending on the endpoint, but the object structure will persist.
    }

    [Serializable]
    public struct ErrorObject
    {
        // - Fields -
        public MessageObject error;
    }

    [Serializable]
    public struct ObjectArray<T>
    {
        // - Fields -
        public int result_count;    // Number of results returned in the current request.
        public int result_limit;    // Maximum number of results returned. Defaults to 100 unless overridden by _limit.
        public int result_offset;   // Number of results skipped over. Defaults to 1 unless overridden by _offset.
        public T[] data; // Contains all data returned from the request
    }
}
