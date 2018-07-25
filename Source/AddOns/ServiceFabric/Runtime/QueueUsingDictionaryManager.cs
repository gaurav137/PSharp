#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;
    using System.Collections.Concurrent;

    public class QueueUsingDictionaryManager : IQueueUsingDictionaryManager
    {
        private readonly ConcurrentDictionary<string, object> queuesUsingDictionary
            = new ConcurrentDictionary<string, object>();

        public IQueueUsingDictionary<T> GetOrCreateAsync<T>(string name, IReliableDictionary<long, T> backingDictionary)
        {
            var wrappedQueue = queuesUsingDictionary.GetOrAdd(name, x => new QueueUsingDictionary<T>(name, backingDictionary));
            return (IQueueUsingDictionary<T>)wrappedQueue;
        }
    }
}
