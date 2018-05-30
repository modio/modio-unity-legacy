namespace ModIO
{
    public enum GameModCuration
    {
        // Mods are immediately available to play
        None = 0,
        // Mods are immediately available to play unless they choose to receive donations.
        // These mods must be accepted to be listed
        Paid = 1,
        // All mods must be accepted by someone to be listed
        Full = 2,
    }
}
