namespace ModIO
{
    [System.Serializable]
    public struct ModfileIdPair
    {
        // ---------[ Constants ]---------
        public static readonly ModfileIdPair NULL =
            new ModfileIdPair(ModProfile.NULL_ID, Modfile.NULL_ID);

        public int modId;
        public int modfileId;

        // ---------[ INITIALIZATION ]---------
        public ModfileIdPair(int modId, int modfileId)
        {
            this.modId = modId;
            this.modfileId = modfileId;
        }
    }
}
