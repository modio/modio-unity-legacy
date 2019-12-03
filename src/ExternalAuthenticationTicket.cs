namespace ModIO
{
    /// <summary>Data for an external authentication ticket.</summary>
    public struct ExternalAuthenticationTicket
    {
        /// <summary>Provider of the ticket.</summary>
        public ExternalAuthenticationProvider provider;

        /// <summary>Base64 encoded ticket value.</summary>
        public string value;
    }
}
