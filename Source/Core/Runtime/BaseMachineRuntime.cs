//-----------------------------------------------------------------------
// <copyright file="BaseMachineRuntime.cs">
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
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The base P# runtime.
    /// </summary>
    internal abstract class BaseMachineRuntime : BaseRuntime
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        internal BaseMachineRuntime(Configuration configuration)
            : base(configuration)
        { }

        #region runtime interface

        /// <summary>
        /// Creates a fresh machine id that has not yet been bound to any machine.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <returns>MachineId</returns>

        public MachineId CreateMachineId(Type type, string friendlyName = null) => new MachineId(this, type, friendlyName);

        #endregion

        #region state-machine execution

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        protected internal MachineId CreateMachine(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            Machine machine = this.CreateMachine(mid, type, friendlyName);
            base.Logger.OnCreateMachine(machine.Id, creator?.Id);
            this.SetOperationGroupIdForMachine(machine, creator, operationGroupId);
            this.RunMachineEventHandler(machine, e, true);
            return machine.Id;
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the created machine reaches quiescence
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        protected internal async Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string friendlyName, Event e,
            Machine creator, Guid? operationGroupId)
        {
            Machine machine = this.CreateMachine(mid, type, friendlyName);
            base.Logger.OnCreateMachine(machine.Id, creator?.Id);
            this.SetOperationGroupIdForMachine(machine, creator, operationGroupId);
            await this.RunMachineEventHandlerAsync(machine, e, true);
            return machine.Id;
        }

        /// <summary>
        /// Creates a new remote <see cref="Machine"/> of the specified <see cref="System.Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        protected internal MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint,
            Event e, Machine creator, Guid? operationGroupId)
        {
            base.Assert(type.IsSubclassOf(typeof(Machine)), $"Type '{type.Name}' is not a machine.");
            return base.NetworkProvider.RemoteCreateMachine(type, friendlyName, endpoint, e);
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <returns>Machine</returns>
        private Machine CreateMachine(MachineId mid, Type type, string friendlyName)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), "Type '{0}' is not a machine.", type.Name);

            if (mid == null)
            {
                mid = new MachineId(this, type, friendlyName);
            }
            else
            {
                this.Assert(mid.Runtime == null || mid.Runtime == this, "Unbound machine id '{0}' was created by another runtime.", mid.Name);
                this.Assert(mid.Type == type.FullName, "Cannot bind machine id '{0}' of type '{1}' to a machine of type '{2}'.",
                    mid.Name, mid.Type, type.FullName);
                mid.Bind(this);
            }

            Machine machine = this.CreateMachine(type);

            machine.Initialize(this, mid, new MachineInfo(mid));
            machine.InitializeStateInformation();

            bool result = this.MachineMap.TryAdd(mid, machine);
            this.Assert(result, "MachineId {0} = This typically occurs " +
                "either if the machine id was created by another runtime instance, or if a machine id from a previous " +
                "runtime generation was deserialized, but the current runtime has not increased its generation value.",
                mid.Name);

            return machine;
        }

        /// <summary>
        /// Creates a new P# machine of the specified type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Machine</returns>
        protected Machine CreateMachine(Type type)
        {
            return MachineFactory.Create(type);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        protected internal void SendEvent(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            var operationGroupId = base.GetNewOperationGroupId(sender, options?.OperationGroupId);
            if (!base.GetTargetMachine(mid, e, sender, operationGroupId, out Machine machine))
            {
                return;
            }

            bool runNewHandler = false;
            this.EnqueueEvent(machine, e, sender, operationGroupId, ref runNewHandler);
            if (runNewHandler)
            {
                this.RunMachineEventHandler(machine, null, false);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine. Returns immediately
        /// if the target machine was already running. Otherwise blocks until the machine handles
        /// the event and reaches quiescense again.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>True if event was handled, false if the event was only enqueued</returns>
        protected internal async Task<bool> SendEventAndExecute(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            var operationGroupId = base.GetNewOperationGroupId(sender, options?.OperationGroupId);
            if (!base.GetTargetMachine(mid, e, sender, operationGroupId, out Machine machine))
            {
                return true;
            }

            bool runNewHandler = false;
            this.EnqueueEvent(machine, e, sender, operationGroupId, ref runNewHandler);
            if (runNewHandler)
            {
                await this.RunMachineEventHandlerAsync(machine, null, false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        protected internal void SendEventRemotely(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            base.NetworkProvider.RemoteSend(mid, e);
        }

        /// <summary>
        /// Enqueues an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="runNewHandler">Run a new handler</param>
        private void EnqueueEvent(Machine machine, Event e, AbstractMachine sender, Guid operationGroupId, ref bool runNewHandler)
        {
            EventInfo eventInfo = new EventInfo(e, null);
            eventInfo.SetOperationGroupId(operationGroupId);

            var senderState = (sender as Machine)?.CurrentStateName ?? string.Empty;
            base.Logger.OnSend(machine.Id, sender?.Id, senderState,
                e.GetType().FullName, operationGroupId, isTargetHalted: false);

            machine.Enqueue(eventInfo, ref runNewHandler);
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">Machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        protected void RunMachineEventHandler(Machine machine, Event initialEvent, bool isFresh)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await machine.GotoStartState(initialEvent);
                    }

                    await machine.RunEventHandler();
                }
                catch (Exception ex)
                {
                    base.IsRunning = false;
                    base.RaiseOnFailureEvent(ex);
                    this.Dispose();
                }
            });
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// </summary>
        /// <param name="machine">Machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        protected async Task RunMachineEventHandlerAsync(Machine machine, Event initialEvent, bool isFresh)
        {
            try
            {
                if (isFresh)
                {
                    await machine.GotoStartState(initialEvent);
                }

                await machine.RunEventHandler();
            }
            catch (Exception ex)
            {
                base.IsRunning = false;
                base.RaiseOnFailureEvent(ex);
                this.Dispose();
                return;
            }
        }

        #endregion
    }
}
