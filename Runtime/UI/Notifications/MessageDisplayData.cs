namespace ModIO.UI
{
    [System.Serializable]
    public struct MessageDisplayData
    {
        public enum Type
        {
            Info,
            Success,
            Warning,
            Error,
        }

        public Type type;
        public string content;
        public float displayDuration;
    }
}
