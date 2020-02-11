namespace ModIO.UI
{
    public interface IModRatingAddedReceiver
    {
        void OnModRatingAdded(int modId, ModRatingValue rating);
    }
}
