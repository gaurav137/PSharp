#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System.Threading.Tasks;

    public static class ReliableStateManagerExtensions
    {
        private static readonly IReactiveReliableQueueManager _reactiveReliableQueueManager = new ReactiveReliableQueueManager();
        private static readonly ICounterManager _counterManager = new CounterManager();
        private static readonly IDeferrableReliableQueueManager _deferrableReliableQueueManager = new DeferrableReliableQueueManager();
        private static readonly IQueueUsingDictionaryManager _queueUsingDictionaryManager = new QueueUsingDictionaryManager();

        public static async Task<IReactiveReliableQueue<T>> GetOrAddReactiveReliableQueue<T>(this IReliableStateManager reliableStateManager, string name)
        {
            var queue = await reliableStateManager.GetOrAddAsync<IReliableQueue<T>>(name);
            return _reactiveReliableQueueManager.GetOrCreateAsync(queue);
        }

        public static async Task<ICounter> GetOrAddCounter(this IReliableStateManager reliableStateManager, string name)
        {
            var backingDictionary = await reliableStateManager.GetOrAddAsync<IReliableDictionary<string, long>>(name)
                .ConfigureAwait(false);

            return _counterManager.GetOrCreateAsync(backingDictionary);
        }

        public static async Task<IDeferrableReliableQueue<T>> GetOrAddDeferrableReliableQueue<T>(this IReliableStateManager reliableStateManager, string name)
        {
            var backingQueue = await reliableStateManager.GetOrAddAsync<IReliableQueue<T>>($"{name}-backingQueue").ConfigureAwait(false);
            var backingDictionary = await reliableStateManager.GetOrAddAsync<IReliableDictionary<long, T>>($"{name}-backingDictionary").ConfigureAwait(false);
            var queueStateCounter = await reliableStateManager.GetOrAddCounter($"{name}-queueStateCounter").ConfigureAwait(false);
            var headCounter = await reliableStateManager.GetOrAddCounter($"{name}-headCounter").ConfigureAwait(false);
            var tailCounter = await reliableStateManager.GetOrAddCounter($"{name}-tailCounter").ConfigureAwait(false);

            IDeferrableReliableQueue<T> deferrableReliableQueue;

            using (var tx = reliableStateManager.CreateTransaction())
            {
                await headCounter.SetCounterValue(tx, 0);
                await tailCounter.SetCounterValue(tx, 0);
                deferrableReliableQueue = _deferrableReliableQueueManager.GetOrCreateAsync<T>(name, backingQueue, backingDictionary, queueStateCounter, headCounter, tailCounter);
                await queueStateCounter.SetCounterValue(tx, deferrableReliableQueue.NeverDeferred);
                await tx.CommitAsync();
            }

            return deferrableReliableQueue;
        }

        public static async Task<IQueueUsingDictionary<T>> GetOrAddQueueAsDictionary<T>(this IReliableStateManager reliableStateManager, string name)
        {
            var backingDictionary = await reliableStateManager.GetOrAddAsync<IReliableDictionary<long, T>>($"{name}-backingDictionary").ConfigureAwait(false);
            var queueUsingDictionary = _queueUsingDictionaryManager.GetOrCreateAsync<T>(name, backingDictionary);
            reliableStateManager.StateManagerChanged += queueUsingDictionary.OnStateManagerChangedHandler;
            return queueUsingDictionary;
        }

    }
}
