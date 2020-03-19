namespace ModIO
{
    /// <summary>Describes a reason for a content report submission.</summary>
    public enum ReportType
    {
        DMCA = 1,
        NotWorking = 2,
        RudeContent = 3,
        IllegalContent = 4,
        StolenContent = 5,
        FalseInformation = 6,
        Other = 7,
    }
}
