namespace ModIO
{
    [System.Serializable]
    public class EditableModfile
    {
        // ---------[ FIELDS ]---------
        public EditableStringField version        = new EditableStringField();
        public EditableStringField changelog      = new EditableStringField();
        public EditableStringField metadataBlob   = new EditableStringField();
    }
}