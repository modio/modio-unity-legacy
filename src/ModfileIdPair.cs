namespace ModIO
{
    [System.Serializable]
    public struct ModfileIdPair
    {
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
