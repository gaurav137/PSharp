#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data;
    using System.Threading.Tasks;

    public interface ICounter
    {
        Task<long> IncrementCounter(ITransaction tx);

        Task<long> SetCounterValue(ITransaction tx, long value);

        Task<long> GetCounterValue(ITransaction tx);
    }
}
