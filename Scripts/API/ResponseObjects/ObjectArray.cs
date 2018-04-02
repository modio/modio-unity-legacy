namespace ModIO.API
{
    [System.Serializable]
    public struct ObjectArray<T>
    {
        // Number of results returned in the current request.
        public int result_count;
        // Maximum number of results returned. Defaults to 100 unless overridden by _limit.
        public int result_limit;
        // Number of results skipped over. Defaults to 1 unless overridden by _offset.
        public int result_offset;
        // Contains all data returned from the request
        public T[] data;
    }
}