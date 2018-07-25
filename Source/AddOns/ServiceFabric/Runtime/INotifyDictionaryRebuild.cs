
namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Data.Notifications;
    using System;
    using System.Threading.Tasks;

    public interface INotifyDictionaryRebuild<K, V> where K : IComparable<K>, IEquatable<K>
    {
        void OnStateManagerChangedHandler(object sender, NotifyStateManagerChangedEventArgs e);

        Task OnDictionaryRebuildNotificationHandlerAsync(IReliableDictionary<K, V> origin, NotifyDictionaryRebuildEventArgs<K, V> rebuildNotification);

    }
}