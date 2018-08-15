﻿//-----------------------------------------------------------------------
// <copyright file="BaseProductionRuntime.cs">
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
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// The base P# production runtime.
    /// </summary>
    internal abstract class BaseProductionRuntime : BaseRuntime
    {
        /// <summary>
        /// True if testing mode is enabled, else false.
        /// </summary>
        public override bool IsTestingModeEnabled => false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        internal BaseProductionRuntime(Configuration configuration)
            : base(configuration)
        { }

        #region machine creation and execution

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override Task<MachineId> CreateMachineAsync(Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAsync(null, type, null, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override Task<MachineId> CreateMachineAsync(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAsync(null, type, friendlyName, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override Task<MachineId> CreateMachineAsync(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAsync(mid, type, null, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecuteAsync(null, type, null, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecuteAsync(null, type, friendlyName, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecuteAsync(mid, type, null, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="e">Event passed during machine construction.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="creator">The creator machine.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override async Task<MachineId> CreateMachineAsync(MachineId mid, Type type, string friendlyName,
            Event e, IMachine creator, Guid? operationGroupId)
        {
            this.Assert(this.IsSupportedMachineType(type), "Type '{0}' is not a machine.", type.Name);
            IMachine machine = await this.CreateMachineAsync(mid, type, friendlyName);
            this.Logger.OnCreateMachine(machine.Id, creator?.Id);
            this.SetOperationGroupIdForMachine(machine, creator, operationGroupId);
            this.RunMachineEventHandler(machine, e, true);
            return machine.Id;
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>. The
        /// method returns only when the created machine reaches quiescence
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="e">Event passed during machine construction.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="creator">The creator machine.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        private async Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, string friendlyName,
            Event e, IMachine creator, Guid? operationGroupId)
        {
            this.Assert(this.IsSupportedMachineType(type), "Type '{0}' is not a machine.", type.Name);
            IMachine machine = await this.CreateMachineAsync(mid, type, friendlyName);
            this.Logger.OnCreateMachine(machine.Id, creator?.Id);
            this.SetOperationGroupIdForMachine(machine, creator, operationGroupId);
            await this.RunMachineEventHandlerAsync(machine, e, true);
            return machine.Id;
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine.</returns>
        protected async Task<IMachine> CreateMachineAsync(MachineId mid, Type type, string friendlyName)
        {
            if (mid == null)
            {
                mid = this.CreateMachineId(type, friendlyName);
            }
            else
            {
                this.Assert(mid.Runtime == null || mid.Runtime == this,
                    "Unbound machine id '{0}' was created by another runtime.", mid.Name);
                this.Assert(mid.Type == type.FullName, "Cannot bind machine id '{0}' of type '{1}' to a machine of type '{2}'.",
                    mid.Name, mid.Type, type.FullName);
                mid.Bind(this);
            }

            IMachine machine = await this.CreateMachineAsync(mid, type);

            bool result = this.MachineMap.TryAdd(mid, machine);
            this.Assert(result, "MachineId {0} = This typically occurs " +
                "either if the machine id was created by another runtime instance, or if a machine id from a previous " +
                "runtime generation was deserialized, but the current runtime has not increased its generation value.",
                mid.Name);

            return machine;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public override Task SendEventAsync(MachineId target, Event e, SendOptions options = null)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            return this.SendEventAsync(target, e, null, options);
        }

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense again.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is true if
        /// the event was handled, false if the event was only enqueued.</returns>
        public override Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, SendOptions options = null)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            return this.SendEventAndExecuteAsync(target, e, null, options);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">The sender machine.</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public override async Task SendEventAsync(MachineId mid, Event e, IMachine sender, SendOptions options)
        {
            var operationGroupId = this.GetNewOperationGroupId(sender, options?.OperationGroupId);
            if (this.GetTargetMachine(mid, e, sender, operationGroupId, out IMachine machine))
            {
                MachineStatus machineStatus = await this.EnqueueEventAsync(machine, e, sender, operationGroupId);
                if (machineStatus == MachineStatus.EventHandlerNotRunning)
                {
                    this.RunMachineEventHandler(machine, null, false);
                }
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine. Returns immediately
        /// if the target machine was already running. Otherwise blocks until the machine handles
        /// the event and reaches quiescense again.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">The sender machine.</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is true if
        /// the event was handled, false if the event was only enqueued.</returns>
        private async Task<bool> SendEventAndExecuteAsync(MachineId mid, Event e, IMachine sender, SendOptions options)
        {
            var operationGroupId = this.GetNewOperationGroupId(sender, options?.OperationGroupId);
            if (!this.GetTargetMachine(mid, e, sender, operationGroupId, out IMachine machine))
            {
                return true;
            }

            MachineStatus machineStatus = await this.EnqueueEventAsync(machine, e, sender, operationGroupId);
            if (machineStatus == MachineStatus.EventHandlerNotRunning)
            {
                await this.RunMachineEventHandlerAsync(machine, null, false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enqueues an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="e">Event</param>
        /// <param name="sender">The sender machine.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is the machine status after the enqueue.
        /// </returns>
        protected Task<MachineStatus> EnqueueEventAsync(IMachine machine, Event e, IMachine sender, Guid operationGroupId)
        {
            EventInfo eventInfo = new EventInfo(e, null);
            eventInfo.SetOperationGroupId(operationGroupId);
            this.Logger.OnSend(machine.Id, sender?.Id, sender?.CurrentStateName ?? String.Empty,
                e.GetType().FullName, operationGroupId, isTargetHalted: false);
            return machine.EnqueueAsync(eventInfo, sender);
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">The machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        protected void RunMachineEventHandler(IMachine machine, Event initialEvent, bool isFresh)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await machine.GotoStartStateAsync(initialEvent);
                    }

                    await machine.RunEventHandlerAsync();
                }
                catch (Exception ex)
                {
                    this.IsRunning = false;
                    this.RaiseOnFailureEvent(ex);
                    this.Dispose();
                }
            });
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// </summary>
        /// <param name="machine">The machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        protected async Task RunMachineEventHandlerAsync(IMachine machine, Event initialEvent, bool isFresh)
        {
            try
            {
                if (isFresh)
                {
                    await machine.GotoStartStateAsync(initialEvent);
                }

                await machine.RunEventHandlerAsync();
            }
            catch (Exception ex)
            {
                this.IsRunning = false;
                this.RaiseOnFailureEvent(ex);
                this.Dispose();
                return;
            }
        }

        #endregion

        #region timers

        /// <summary>
        /// Return the timer machine type
        /// </summary>
        /// <returns></returns>
        public override Type GetTimerMachineType()
        {
            var timerType = base.GetTimerMachineType();
            if (timerType == null)
            {
                return typeof(Timers.ProductionTimerMachine);
            }

            return timerType;
        }

        #endregion

        #region nondeterministic choices

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>Boolean</returns>
        public override bool GetNondeterministicBooleanChoice(IMachine machine, int maxValue)
        {
            Random random = new Random(DateTime.Now.Millisecond);

            bool result = false;
            if (random.Next(maxValue) == 0)
            {
                result = true;
            }

            this.Logger.OnRandom(machine?.Id, result);

            return result;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        public override bool GetFairNondeterministicBooleanChoice(IMachine machine, string uniqueId)
        {
            return this.GetNondeterministicBooleanChoice(machine, 2);
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>Integer</returns>
        public override int GetNondeterministicIntegerChoice(IMachine machine, int maxValue)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            var result = random.Next(maxValue);

            this.Logger.OnRandom(machine?.Id, result);

            return result;
        }

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        public override void NotifyEnteredState(IMachine machine)
        {
            if (this.Configuration.Verbose <= 1)
            {
                return;
            }

            this.Logger.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: true);
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        public override void NotifyEnteredState(Monitor monitor)
        {
            if (this.Configuration.Verbose <= 1)
            {
                return;
            }

            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.Logger.OnMonitorState(monitor.GetType().Name, monitor.Id, monitorState, true, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        public override void NotifyExitedState(IMachine machine)
        {
            if (this.Configuration.Verbose <= 1)
            {
                return;
            }

            this.Logger.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: false);
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        public override void NotifyExitedState(Monitor monitor)
        {
            if (this.Configuration.Verbose <= 1)
            {
                return;
            }

            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.Logger.OnMonitorState(monitor.GetType().Name, monitor.Id, monitorState, false, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        public override void NotifyInvokedAction(IMachine machine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.Verbose <= 1)
            {
                return;
            }

            this.Logger.OnMachineAction(machine.Id, machine.CurrentStateName, action.Name);
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        public override void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.Verbose <= 1)
            {
                return;
            }

            this.Logger.OnMonitorAction(monitor.GetType().Name, monitor.Id, action.Name, monitor.CurrentStateName);
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        public override void NotifyRaisedEvent(IMachine machine, EventInfo eventInfo)
        {
            eventInfo.SetOperationGroupId(this.GetNewOperationGroupId(machine, null));

            if (this.Configuration.Verbose <= 1)
            {
                return;
            }

            this.Logger.OnMachineEvent(machine.Id, machine.CurrentStateName, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="eventInfo">The event metadata.</param>
        public override void NotifyRaisedEvent(Monitor monitor, EventInfo eventInfo)
        {
            if (this.Configuration.Verbose <= 1)
            {
                return;
            }

            this.Logger.OnMonitorEvent(monitor.GetType().Name, monitor.Id, monitor.CurrentStateName,
                eventInfo.EventName, isProcessing: false);
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        public override void NotifyDequeuedEvent(IMachine machine, EventInfo eventInfo)
        {
            // The machine inherits the operation group id of the dequeued event.
            machine.Info.OperationGroupId = eventInfo.OperationGroupId;
            this.Logger.OnDequeue(machine.Id, machine.CurrentStateName, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfoInInbox">The event info if it is in the inbox, else null</param>
        /// <param name="eventNames">The names of the events that the machine is waiting for.</param>
        public override void NotifyWaitEvents(IMachine machine, EventInfo eventInfoInInbox, string eventNames)
        {
            if (eventInfoInInbox == null)
            {
                this.Logger.OnWait(machine.Id, machine.CurrentStateName, String.Empty);
                machine.Info.IsWaitingToReceive = true;
            }
        }

        /// <summary>
        /// Notifies that a machine received an <see cref="Event"/> that it was waiting for.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        public override void NotifyReceivedEvent(IMachine machine, EventInfo eventInfo)
        {
            this.Logger.OnReceive(machine.Id, machine.CurrentStateName, eventInfo.EventName, wasBlocked: true);

            lock (machine)
            {
                System.Threading.Monitor.Pulse(machine);
                machine.Info.IsWaitingToReceive = false;
            }
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public override Task NotifyHaltedAsync(IMachine machine)
        {
            this.MachineMap.TryRemove(machine.Id, out machine);
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        #endregion

        #region operation group id

        /// <summary>
        /// Returns the operation group id of the specified machine id. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="MachineId"/> is not associated with this runtime.
        /// During testing, the runtime asserts that the specified machine is currently executing.
        /// </summary>
        /// <param name="currentMachine">MachineId of the currently executing machine.</param>
        /// <returns>Guid</returns>
        public override Guid GetCurrentOperationGroupId(MachineId currentMachine)
        {
            if (!this.MachineMap.TryGetValue(currentMachine, out IMachine machine))
            {
                return Guid.Empty;
            }

            return machine.Info.OperationGroupId;
        }

        #endregion
    }
}
