using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;

namespace Microsoft.PSharp.ServiceFabric
{
    class QueueUsingDictionary<V> : IQueueUsingDictionary<V>
    {
        private IReliableDictionary<long, V> backingDictionary;
        private long head;
        private long tail;
        private string name;

        public QueueUsingDictionary(string name, IReliableDictionary<long, V> backingDictionary)
        {
            this.name = name;
            this.backingDictionary = backingDictionary;
            head = 0;
            tail = 0;
        }

        public long Count => throw new NotImplementedException();

        public Uri Name => throw new NotImplementedException();

        public async Task EnqueueAsync(ITransaction tx, V value, CancellationToken cancellationToken = default(CancellationToken), TimeSpan? timeout = null)
        {
            var insertPos = Interlocked.Increment(ref tail);
            await backingDictionary.AddOrUpdateAsync(tx, insertPos, x => value, (x, y) => value);
        }

        public async Task OnDictionaryRebuildNotificationHandlerAsync(IReliableDictionary<long, V> origin, NotifyDictionaryRebuildEventArgs<long, V> rebuildNotification)
        {
            var enumerator = rebuildNotification.State.GetAsyncEnumerator();
            long lowIndex = long.MaxValue;
            long highIndex = long.MinValue;
            while (await enumerator.MoveNextAsync(CancellationToken.None))
            {
                long current = enumerator.Current.Key;

                if (current > highIndex)
                {
                    highIndex = current;
                }

                if (current < lowIndex)
                {
                    lowIndex = current;
                }
            }

            head = lowIndex;
            tail = highIndex;
        }

        public void OnStateManagerChangedHandler(object sender, NotifyStateManagerChangedEventArgs e)
        {
            var operation = e as NotifyStateManagerSingleEntityChangedEventArgs;

            if (operation != null && operation.Action == NotifyStateManagerChangedAction.Add)
            {
                if (operation.ReliableState is IReliableDictionary<long, V>)
                {
                    var dictionary = (IReliableDictionary<long, V>)operation.ReliableState;
                    dictionary.RebuildNotificationAsyncCallback = OnDictionaryRebuildNotificationHandlerAsync;
                }
            }
        }

        public async Task<ConditionalValue<V>> TryDequeueAsync(ITransaction tx, CancellationToken cancellationToken = default(CancellationToken), TimeSpan? timeout = null)
        {
            if (head >= tail)
            {
                return new ConditionalValue<V>(false, default(V));
            }

            ConditionalValue<V> item;
            do
            {
                item = await backingDictionary.TryRemoveAsync(tx, head);
                Interlocked.Increment(ref head);
                if (head == tail)
                {
                    return new ConditionalValue<V>(false, default(V));
                }
            } while (!item.HasValue);

            return item;
        }
    }
}
