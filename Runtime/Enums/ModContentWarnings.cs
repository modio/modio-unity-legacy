namespace ModIO
{
    [System.Flags]
    public enum ModContentWarnings
    {
        None = 0x00,
        Alcohol = 0x01,
        Drugs = 0x02,
        Violence = 0x04,
        Explicit = 0x08,
    }
}
