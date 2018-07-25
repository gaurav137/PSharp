#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;

    public interface IQueueUsingDictionaryManager
    {
        IQueueUsingDictionary<V> GetOrCreateAsync<V>(string name, IReliableDictionary<long, V> backingDictionary);
    }
}
