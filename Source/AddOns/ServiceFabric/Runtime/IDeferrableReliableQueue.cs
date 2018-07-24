#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IDeferrableReliableQueue<V> : IReliableState
    {

        long NeverDeferred { get; }

        long HasDeferred { get; }

        Task EnqueueAsync(ITransaction tx, V item);

        Task<ConditionalValue<V>> TryDequeueAsync(ITransaction tx, CancellationToken cancellationToken);

        Task<ConditionalValue<V>> TryDequeueDeferredAsync(ITransaction tx, CancellationToken cancellationToken, Func<V, HashSet<Type>, bool> shouldDefer, HashSet<Type> deferredTypes);

        Task<long> GetCountAsync(ITransaction tx);
    }
}
