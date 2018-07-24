#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class DeferrableReliableQueue<V> : IDeferrableReliableQueue<V>
    {
        private readonly string name;
        private readonly IReliableQueue<V> backingQueue;
        private readonly IReliableDictionary<long, V> backingDictionary;
        private readonly ICounter queueState;
        private readonly ICounter head;
        private readonly ICounter tail;

        private Func<V, HashSet<Type>, bool> DoNotDefer = (V val, HashSet<Type> typesToDefer) => false;

        public DeferrableReliableQueue(
            string name,
            IReliableQueue<V> backingQueue,
            IReliableDictionary<long, V> backingDictionary,
            ICounter queueStateCounter,
            ICounter headCounter,
            ICounter tailCounter)
        {
            this.name = name;
            this.backingQueue = backingQueue;
            this.backingDictionary = backingDictionary;
            this.queueState = queueStateCounter;
            this.head = headCounter;
            this.tail = tailCounter;
        }

        public long NeverDeferred { get => 0; }

        public long HasDeferred { get => 1; }

        public Uri Name => new Uri(name);

        public async Task EnqueueAsync(ITransaction tx, V item)
        {
            await backingQueue.EnqueueAsync(tx, item).ConfigureAwait(false);
        }

        public Task<long> GetCountAsync(ITransaction tx)
        {
            throw new NotImplementedException();
        }

        public async Task<ConditionalValue<V>> TryDequeueAsync(ITransaction tx, CancellationToken cancellationToken)
        {
            long queueStateFlag = await this.queueState.GetCounterValue(tx);
            if (queueStateFlag == NeverDeferred)
            {
                return await backingQueue.TryDequeueAsync(tx).ConfigureAwait(false);
            }
            else
            {
                return await TryDequeueDeferredAsync(tx, cancellationToken, DoNotDefer, null).ConfigureAwait(false);
            }
        }

        public async Task<ConditionalValue<V>> TryDequeueDeferredAsync(ITransaction tx, CancellationToken cancellationToken, Func<V, HashSet<Type>, bool> shouldDefer, HashSet<Type> deferredTypes)
        {
            long queueStateFlag = await this.queueState.GetCounterValue(tx);
            if (queueStateFlag == NeverDeferred)
            {
                await queueState.SetCounterValue(tx, HasDeferred);
            }

            ConditionalValue<V> valueToReturn = new ConditionalValue<V>(false, default(V));
            var headPosition = await head.GetCounterValue(tx);
            var tailPosition = await tail.GetCounterValue(tx);

            bool queueHasElements = false;

            ConditionalValue<V> currentItem;
            while (true)
            {
                currentItem = await backingQueue.TryDequeueAsync(tx);
                if (!currentItem.HasValue)
                {
                    break;
                }

                queueHasElements = true;
                await backingDictionary.AddOrUpdateAsync(tx, tailPosition, x => currentItem.Value, (x, y) => currentItem.Value);
                tailPosition = (tailPosition + 1);
                if (tailPosition == long.MaxValue - 1)
                {
                    throw new Exception("Capacity reached");
                }
            }

            if (queueHasElements)
            {
                await tail.SetCounterValue(tx, tailPosition);
            }

            for (long i = headPosition; i < tailPosition; i++)
            {
                var item = await backingDictionary.TryGetValueAsync(tx, i);
                if (item.HasValue)
                {
                    if (shouldDefer(item.Value, deferredTypes))
                    {
                        continue;
                    }

                    await backingDictionary.TryRemoveAsync(tx, i);
                    return item;
                }
            }

            return valueToReturn;
        }
    }
}
