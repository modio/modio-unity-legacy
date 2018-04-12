namespace ModIO.API
{
    [System.Serializable]
    public struct MessageObject
    {
        // HTTP status code of response.
        public int code;
        // The server response to your request. Responses will vary depending on the endpoint, but the object structure will persist.
        public string message;
    }
}