#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;

    public interface IDeferrableReliableQueueManager
    {
        IDeferrableReliableQueue<V> GetOrCreateAsync<V>(
            string name,
            IReliableQueue<V> backingQueue,
            IReliableDictionary<long, V> backingDictionary,
            ICounter queueStateCounter,
            ICounter headCounter,
            ICounter tailCounter);
    }
}
