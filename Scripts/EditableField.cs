namespace ModIO
{
    [System.Serializable]
    public class EditableField<T>
    {
        public T value = default(T);
        public bool isDirty = false;
    }
}