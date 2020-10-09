namespace ModIO.UI
{
    /// <summary>Allows a MonoBehaviour to be assigned to a ModView.</summary>
    public interface IModViewElement
    {
        UnityEngine.GameObject gameObject { get; }

        void SetModView(ModView view);
    }
}
