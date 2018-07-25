
namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;

    public interface IQueueUsingDictionary<V> : IReliableConcurrentQueue<V>, INotifyDictionaryRebuild<long, V>
    {

    }
}
