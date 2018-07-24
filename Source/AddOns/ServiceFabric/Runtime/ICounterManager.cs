#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;

    public interface ICounterManager
    {
        ICounter GetOrCreateAsync(IReliableDictionary<string, long> dictionary);
    }
}
