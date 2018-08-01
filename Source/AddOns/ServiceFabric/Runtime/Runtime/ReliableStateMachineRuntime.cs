﻿//-----------------------------------------------------------------------
// <copyright file="ReliableStateMachineRuntime.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    internal class ReliableStateMachineRuntime : StateMachineRuntime, IReliableStateMachineRuntime
    {
        private const string CreatedMachinesDictionaryName = "CreatedMachines";
        private const string RemoteMessagesOutboxName = "RemoteMessagesOutbox";
        private const string RemoteCreationsOutboxName = "RemoteCreationsOutbox";

        /// <summary>
        /// State Manager
        /// </summary>
        IReliableStateManager StateManager;

        /// <summary>
        /// Service cancellation token
        /// </summary>
        internal CancellationToken ServiceCancellationToken;

        /// <summary>
        /// Default time limit for SF operations
        /// </summary>
        internal TimeSpan DefaultTimeLimit;

        /// <summary>
        /// Pending machine creations
        /// </summary>
        Dictionary<ITransaction, List<Tuple<MachineId, Type, Event, MachineId>>> PendingMachineCreations;

        /// <summary>
        /// Pending machine deletions (for halted machines)
        /// </summary>
        Dictionary<ITransaction, List<MachineId>> PendingMachineDeletions;

        /// <summary>
        /// RSM network provider
        /// </summary>
        Net.IRsmNetworkProvider RsmNetworkProvider;

        /// <summary>
        /// The remote machine manager used for creating/sending messages
        /// </summary>
        public IRemoteMachineManager RemoteMachineManager { get; }

        #region runtime interface

        /// <summary>
        /// Returns the created machine ids.
        /// </summary>
        /// <returns>MachineIds</returns>
        public HashSet<MachineId> GetCreatedMachineIds()
        {
            //TODO: Plumb in cancellation token
            //TODO: Do not report halted machines - we need some way to delete the halted machines and clean up the IDs/queues
            return this.GetCreatedMachinesAsync(CancellationToken.None).Result;
        }

        #endregion

        internal ReliableStateMachineRuntime(IReliableStateManager stateManager, IRemoteMachineManager manager)
            : this(stateManager, manager, Configuration.Create(), CancellationToken.None)
        { }

        internal ReliableStateMachineRuntime(IReliableStateManager stateManager, IRemoteMachineManager manager, Configuration configuration)
            : this(stateManager, manager, configuration, CancellationToken.None)
        { }

        internal ReliableStateMachineRuntime(IReliableStateManager stateManager, IRemoteMachineManager manager, Configuration configuration,
            CancellationToken cancellationToken)
            : base(configuration)
        {
            this.StateManager = stateManager;
            this.ServiceCancellationToken = cancellationToken;
            this.DefaultTimeLimit = TimeSpan.FromSeconds(4);
            this.RemoteMachineManager = manager;
            this.PendingMachineCreations = new Dictionary<ITransaction, List<Tuple<MachineId, Type, Event, MachineId>>>();
            this.PendingMachineDeletions = new Dictionary<ITransaction, List<MachineId>>();
            StartClearOutboxTasks();
        }

        internal void SetRsmNetworkProvider(Net.IRsmNetworkProvider rsmNetworkProvider)
        {
            this.RsmNetworkProvider = rsmNetworkProvider;
            base.SetNetworkProvider(new Net.NullNetworkProvider(this.RemoteMachineManager.GetLocalEndpoint()));
        }

        #region Monitor

        public override void InvokeMonitor<T>(Event e)
        {
            // no-op
        }

        public override void InvokeMonitor(Type type, Event e)
        {
            // no-op
        }

        public override void RegisterMonitor(Type type)
        {
            // no-op
        }

        #endregion

        #region CreateMachine

        protected internal override MachineId CreateMachine(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            if(mid == null)
            {
                var endpoint = RemoteMachineManager.CreateMachineIdEndpoint(type).Result;
                mid = RsmNetworkProvider.RemoteCreateMachineId(type.AssemblyQualifiedName, friendlyName, endpoint).Result;
            }

            if(RemoteMachineManager.IsLocalMachine(mid))
            {
                CreateMachineLocalAsync(mid, type, friendlyName, e, creator, operationGroupId).Wait();
            }
            else
            {
                CreateMachineRemoteAsync(mid, type, friendlyName, e, creator, operationGroupId).Wait();
            }
            return mid;

        }

        internal async Task<MachineId> CreateMachineRemoteAsync(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            var reliableCreator = creator as ReliableMachine;

            if (reliableCreator == null)
            {
                RsmNetworkProvider.RemoteCreateMachine(type, mid, e).Wait();
            }
            else
            {
                var RemoteCreatedMachinesOutbox = await StateManager.GetOrAddAsync<IReliableQueue<Tuple<string, MachineId, Event>>>(RemoteCreationsOutboxName);
                await RemoteCreatedMachinesOutbox.EnqueueAsync(reliableCreator.CurrentTransaction, Tuple.Create(type.AssemblyQualifiedName, mid, e), DefaultTimeLimit, ServiceCancellationToken);
            }

            return mid;
        }

        internal async Task<MachineId> CreateMachineLocalAsync(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            this.Assert(type.IsSubclassOf(typeof(ReliableMachine)), "Type '{0}' is not a reliable machine.", type.Name);
            this.Assert(creator == null || creator is ReliableMachine, "Type '{0}' is not a reliable machine.", creator != null ? creator.GetType().Name : "");

            // Idempotence check
            // TODO: make concurrency safe
            if (MachineMap.ContainsKey(mid))
            {
                // machine already created
                return mid;
            }

            var reliableCreator = creator as ReliableMachine;
            var createdMachineMap = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<MachineId, string, Event>>>(CreatedMachinesDictionaryName);

            if (reliableCreator == null)
            {

                using (var tx = this.StateManager.CreateTransaction())
                {
                    await createdMachineMap.AddAsync(tx, mid.ToString(), Tuple.Create(mid, type.AssemblyQualifiedName, e), DefaultTimeLimit, ServiceCancellationToken);
                    await tx.CommitAsync();
                }
                StartMachine(mid, type, e, creator?.Id);
            }
            else
            {
                this.Assert(reliableCreator.CurrentTransaction != null, "Creator's transaction cannot be null");
                await createdMachineMap.AddAsync(reliableCreator.CurrentTransaction, mid.ToString(), Tuple.Create(mid, type.AssemblyQualifiedName, e), DefaultTimeLimit, ServiceCancellationToken);

                if(!PendingMachineCreations.ContainsKey(reliableCreator.CurrentTransaction))
                {
                    PendingMachineCreations[reliableCreator.CurrentTransaction] = new List<Tuple<MachineId, Type, Event, MachineId>>();
                }
                PendingMachineCreations[reliableCreator.CurrentTransaction].Add(
                    Tuple.Create(mid, type, e, reliableCreator.Id));
            }

            return mid;

        }

        private void StartMachine(MachineId mid, Type type, Event e, MachineId creator)
        {
            // Idempotence check
            // TODO: make concurrency safe
            if (MachineMap.ContainsKey(mid))
            {
                // machine already created
                return;
            }

            this.Assert(mid.Runtime == null || mid.Runtime == this, "Unbound machine id '{0}' was created by another runtime.", mid.Name);
            this.Assert(mid.Type == type.FullName, "Cannot bind machine id '{0}' of type '{1}' to a machine of type '{2}'.",
                mid.Name, mid.Type, type.FullName);
            mid.Bind(this);

            Machine machine = ReliableMachineFactory.Create(type, StateManager);

            machine.Initialize(this, mid, new MachineInfo(mid));
            machine.InitializeStateInformation();

            bool result = this.MachineMap.TryAdd(mid, machine);
            this.Assert(result, "MachineId {0} = This typically occurs " +
                "either if the machine id was created by another runtime instance, or if a machine id from a previous " +
                "runtime generation was deserialized, but the current runtime has not increased its generation value.",
                mid.Name);

            base.Logger.OnCreateMachine(machine.Id, creator);
            this.RunMachineEventHandler(machine, e, true);
        }

        // Restarts created machines (on failover)
        private async Task ReHydrateMachines()
        {
            var createdMachineMap = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<MachineId, string, Event>>>(CreatedMachinesDictionaryName);
            using (var tx = this.StateManager.CreateTransaction())
            {
                var enumerable = await createdMachineMap.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(this.ServiceCancellationToken))
                {
                    ServiceCancellationToken.ThrowIfCancellationRequested();

                    this.Assert(RemoteMachineManager.IsLocalMachine(enumerator.Current.Value.Item1));
                    StartMachine(enumerator.Current.Value.Item1, Type.GetType(enumerator.Current.Value.Item2), enumerator.Current.Value.Item3, null);
                }
            }
        }

        #endregion

        #region Send

        protected internal override void SendEvent(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            // TODO: Make async
            SendEventAsync(mid, e, sender, options).Wait();
        }

        protected internal async Task SendEventAsync(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            var senderState = (sender as Machine)?.CurrentStateName ?? string.Empty;
            base.Logger.OnSend(mid, sender?.Id, senderState,
                e.GetType().FullName, options?.OperationGroupId, isTargetHalted: false);

            var reliableSender = sender as ReliableMachine;
            if (RemoteMachineManager.IsLocalMachine(mid))
            {
                var targetQueue = await StateManager.GetMachineInputQueue(mid);

                if (reliableSender == null || reliableSender.CurrentTransaction == null)
                {
                    // Environment sending to a local machine
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        if (e is TaggedRemoteEvent)
                        {
                            var ReceiveCounters = await StateManager.GetMachineReceiveCounters(mid);
                            var tg = (e as TaggedRemoteEvent);
                            var currentCounter = await ReceiveCounters.GetOrAddAsync(tx, tg.mid.Name, 0);
                            if (currentCounter == tg.tag - 1)
                            {
                                await targetQueue.EnqueueAsync(tx, tg.ev);
                                await ReceiveCounters.AddOrUpdateAsync(tx, tg.mid.Name, 0, (k, v) => tg.tag);
                                await tx.CommitAsync();
                            }
                        }
                        else
                        {
                            await targetQueue.EnqueueAsync(tx, e);
                            await tx.CommitAsync();
                        }
                    }
                }
                else
                {
                    // Machine to machine
                    await targetQueue.EnqueueAsync(reliableSender.CurrentTransaction, e);
                }
            }
            else
            {
                if (reliableSender == null || reliableSender.CurrentTransaction == null)
                {
                    // Environment to remote machine
                    await RsmNetworkProvider.RemoteSend(mid, e);
                }
                else
                {
                    // Machine to remote machine
                    var SendCounters = await StateManager.GetMachineSendCounters(reliableSender.Id);
                    var RemoteMessagesOutbox = await StateManager.GetOrAddAsync<IReliableQueue<Tuple<MachineId, Event>>>(RemoteMessagesOutboxName);

                    var tag = await SendCounters.AddOrUpdateAsync(reliableSender.CurrentTransaction, mid.ToString(), 1, (key, oldValue) => oldValue + 1);
                    var tev = new TaggedRemoteEvent(reliableSender.Id, e, tag);

                    await RemoteMessagesOutbox.EnqueueAsync(reliableSender.CurrentTransaction, Tuple.Create(mid, tev as Event));
                }

            }
        }

        #endregion

        private async Task<HashSet<MachineId>> GetCreatedMachinesAsync(CancellationToken token)
        {
            HashSet<MachineId> list = new HashSet<MachineId>();
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                var createdMachineMap = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<MachineId, string, Event>>>(CreatedMachinesDictionaryName);

                IAsyncEnumerable<KeyValuePair<string, Tuple<MachineId, string, Event>>> enumerable = await createdMachineMap.CreateEnumerableAsync(tx);
                using (IAsyncEnumerator<KeyValuePair<string, Tuple<MachineId, string, Event>>> dictEnumerator = enumerable.GetAsyncEnumerator())
                {
                    while (await dictEnumerator.MoveNextAsync(token))
                    {
                        token.ThrowIfCancellationRequested();

                        list.Add(dictEnumerator.Current.Value.Item1);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Notifies that a machine has progressed. This method can be used to
        /// implement custom notifications based on the specified arguments.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="args">Arguments</param>
        protected internal override async Task NotifyProgress(Machine machine, params object[] args)
        {
            if (args.Length > 0)
            {
                // Halt notification
                if (args[0] is string && (args[0] as string) == "Halt")
                {
                    var ctx = (machine as ReliableMachine).CurrentTransaction;
                    var createdMachineMap = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<MachineId, string, Event>>>(CreatedMachinesDictionaryName);
                    await createdMachineMap.TryRemoveAsync(ctx, machine.Id.ToString());
                    if(!PendingMachineDeletions.ContainsKey(ctx))
                    {
                        PendingMachineDeletions.Add(ctx, new List<MachineId>());
                    }
                    PendingMachineDeletions[ctx].Add(machine.Id);
                }

                // Notifies that a reliable machine has committed its current transaction.
                ITransaction tx = args[0] as ITransaction; // TODO: This can just be the machine's current-tx
                if (tx != null)
                {
                    if (this.Logger.Configuration.Verbose >= this.Logger.LoggingVerbosity)
                    {
                        this.Logger.WriteLine("<CommitLog> Machine '{0}' committed transaction '{1}'.", machine.Id, tx.TransactionId);
                    }

                    if (this.PendingMachineCreations.ContainsKey(tx))
                    {
                        foreach (var tup in this.PendingMachineCreations[tx])
                        {
                            this.StartMachine(tup.Item1, tup.Item2, tup.Item3, tup.Item4);
                        }

                        this.PendingMachineCreations.Remove(tx);
                    }

                    // TODO: This also needs to be done as a "garbage collection" step
                    // in case of failover
                    if(PendingMachineDeletions.ContainsKey(tx))
                    {
                        foreach(var id in PendingMachineDeletions[tx])
                        {
                            await this.StateManager.DeleteMachineInputQueue(id);
                            await this.StateManager.DeleteMachineReceiveCounters(id);
                            await this.StateManager.DeleteMachineSendCounters(id);
                            await this.StateManager.DeleteMachineStackStore(id);
                        }
                    }

                    PendingMachineDeletions.Remove(tx);
                }
            }
        }

        private void StartClearOutboxTasks()
        {
            ReHydrateMachines().Wait();
            Task.Run(async () => await ClearCreationsOutbox());
            Task.Run(async () => await ClearMessagesOutbox());
        }

        private async Task ClearCreationsOutbox()
        {
            var RemoteCreatedMachinesOutbox = await StateManager.GetOrAddAsync<IReliableQueue<Tuple<string, MachineId, Event>>>(RemoteCreationsOutboxName);
            while(true)
            {
                ServiceCancellationToken.ThrowIfCancellationRequested();

                var found = false;
                try
                {
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        var cv = await RemoteCreatedMachinesOutbox.TryDequeueAsync(tx);
                        if (cv.HasValue)
                        {
                            await RsmNetworkProvider.RemoteCreateMachine(Type.GetType(cv.Value.Item1), cv.Value.Item2, cv.Value.Item3);
                            await tx.CommitAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.WriteLine("Exception raised in ClearCreationsOutbox: {0}", ex.ToString());
                }

                if (!found)
                {
                    await Task.Delay(100);
                }
            }
        }

        private async Task ClearMessagesOutbox()
        {
            var RemoteMessagesOutbox = await StateManager.GetOrAddAsync<IReliableQueue<Tuple<MachineId, Event>>>(RemoteMessagesOutboxName);
            while (true)
            {
                ServiceCancellationToken.ThrowIfCancellationRequested();

                var found = false;

                try
                {
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        var cv = await RemoteMessagesOutbox.TryDequeueAsync(tx);
                        if (cv.HasValue)
                        {
                            await RsmNetworkProvider.RemoteSend(cv.Value.Item1, cv.Value.Item2);
                            await tx.CommitAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.WriteLine("Exception raised in ClearMessagesOutbox: {0}", ex.ToString());
                }

                if (!found)
                {
                    await Task.Delay(100);
                }
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public override void Dispose()
        {
            this.PendingMachineCreations.Clear();
            base.Dispose();
        }

        #region Unsupported

        public override Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override Task CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override MachineId RemoteCreateMachine(Type type, string endpoint, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override MachineId RemoteCreateMachine(Type type, string friendlyName, string endpoint, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override void RemoteSendEvent(MachineId target, Event e, SendOptions options = null)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null)
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        protected internal override MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint, Event e, Machine creator, Guid? operationGroupId)
        {
            throw new NotImplementedException();
        }

        protected internal override void SendEventRemotely(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            throw new NotImplementedException();
        }

        protected internal override void TryCreateMonitor(Type type)
        {
            throw new NotImplementedException();
        }

        protected internal override Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            throw new NotImplementedException();
        }


        protected internal override Task<bool> SendEventAndExecute(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member