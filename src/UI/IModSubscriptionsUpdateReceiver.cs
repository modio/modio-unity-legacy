using System.Collections.Generic;

namespace ModIO.UI
{
    public interface IModSubscriptionsUpdateReceiver
    {
        void OnModSubscriptionsUpdated(IList<int> addedSubscriptions,
                                       IList<int> removedSubscriptions);
    }
}
