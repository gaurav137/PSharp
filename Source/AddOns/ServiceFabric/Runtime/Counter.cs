#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System;
    using System.Threading.Tasks;

    public class Counter : ICounter
    {
        private IReliableDictionary<string, long> countDictionary;
        public const string counterName = "counter";

        public Counter(IReliableDictionary<string, long> countDictionary)
        {
            this.countDictionary = countDictionary;
        }

        public async Task<long> IncrementCounter(ITransaction tx)
        {
            // ServiceEventSource.Current.ServiceMessage(this.Context, $"Calling IncrementCounter");
            return await countDictionary.AddOrUpdateAsync(tx, counterName, x => throw new Exception(), (x, y) => y + 1);
        }

        public async Task<long> SetCounterValue(ITransaction tx, long value)
        {
            return await countDictionary.AddOrUpdateAsync(tx, counterName, x => value, (x, y) => value);
        }

        public async Task<long> GetCounterValue(ITransaction tx)
        {
            var conditionalValue = await countDictionary.TryGetValueAsync(tx, counterName);
            return conditionalValue.Value;
        }

    }
}
