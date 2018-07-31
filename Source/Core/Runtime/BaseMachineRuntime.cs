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
        protected internal abstract Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string friendlyName,
            Event e, Machine creator, Guid? operationGroupId);

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
        protected internal abstract MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint,
            Event e, Machine creator, Guid? operationGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        protected internal abstract void SendEvent(MachineId mid, Event e, AbstractMachine sender, SendOptions options);

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
        protected internal abstract Task<bool> SendEventAndExecute(MachineId mid, Event e, AbstractMachine sender, SendOptions options);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        protected internal abstract void SendEventRemotely(MachineId mid, Event e, AbstractMachine sender, SendOptions options);
    }
}
