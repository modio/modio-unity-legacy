namespace ModIO
{
    [System.Flags]
    public enum GameModRevenuePermissions
    {
        // All of the options below are disabled
        None = 0,
        // Allow mods to be sold
        AllowSales = 0x01,
        // Allow mods to receive donations
        AllowDonations = 0x02,
        // Allow mods to be traded
        AllowModTrading = 0x04,
        // Allow mods to control supply and scarcity
        AllowModScarcity = 0x08,
    }
}
