#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;
    using System;
    using System.Collections.Concurrent;

    public class CounterManager : ICounterManager
    {
        private readonly ConcurrentDictionary<Uri, object> allCounters
            = new ConcurrentDictionary<Uri, object>();

        public ICounter GetOrCreateAsync(IReliableDictionary<string, long> backingDictionary)
        {
            var counter = allCounters.GetOrAdd(backingDictionary.Name, x => new Counter(backingDictionary));
            return (ICounter)counter;
        }
    }
}
