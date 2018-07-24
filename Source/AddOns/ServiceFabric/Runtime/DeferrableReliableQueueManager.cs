#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;
    using System;
    using System.Collections.Concurrent;

    public class DeferrableReliableQueueManager : IDeferrableReliableQueueManager
    {
        private readonly ConcurrentDictionary<Uri, object> _deferrableReliableQueues
            = new ConcurrentDictionary<Uri, object>();

        public IDeferrableReliableQueue<V> GetOrCreateAsync<V>(
            string name,
            IReliableQueue<V> backingQueue,
            IReliableDictionary<long, V> backingDictionary,
            ICounter queueStateCounter,
            ICounter headCounter,
            ICounter tailCounter)
        {
            var wrappedQueue = _deferrableReliableQueues.GetOrAdd(backingQueue.Name, x => new DeferrableReliableQueue<V>(name, backingQueue, backingDictionary, queueStateCounter, headCounter, tailCounter));
            return (IDeferrableReliableQueue<V>) wrappedQueue;
        }
    }
}
